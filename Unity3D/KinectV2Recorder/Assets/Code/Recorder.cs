using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Windows.Kinect;
using System.Threading;
using System;

public class Recorder : MonoBehaviour {

    private static KinectSensor sensor;
    private static MultiSourceFrameReader reader;
    private static CoordinateMapper mapper;

    public Texture2D BodyIndexTexture;
    private byte[] indexData;

    public Texture2D ColorTexture;
    private byte[] colorData;

    public Texture2D DepthTexture;
    private ushort[] depthData;
    private byte[] depthPixelData;

    public Texture2D TrackedColorTexture;
    private ColorSpacePoint[] points;
    private byte[] output;

    public static int INDEX_WIDTH = 512;
    public static int INDEX_HEIGHT = 424;
    public static int INDEX_LENGTH = INDEX_WIDTH * INDEX_HEIGHT;

    public SaveQueue Queue;

    public int SaveEveryXFrame = 10;
    private int currentFrame = 0;

    void Start() {
        sensor = KinectSensor.GetDefault();
        sensor.Open();

        BodyIndexTexture = new Texture2D( INDEX_WIDTH, INDEX_HEIGHT, TextureFormat.Alpha8, false );
        BodyIndexTexture.hideFlags = HideFlags.HideAndDontSave;
        BodyIndexTexture.Apply();
        indexData = new byte[INDEX_WIDTH * INDEX_HEIGHT];
        GameObject.Find( "BodyIndex" ).GetComponent<MeshRenderer>().material.mainTexture = BodyIndexTexture;


        ColorTexture = new Texture2D( sensor.ColorFrameSource.FrameDescription.Width, sensor.ColorFrameSource.FrameDescription.Height, TextureFormat.RGBA32, false );
        ColorTexture.hideFlags = HideFlags.HideAndDontSave;
        ColorTexture.Apply();
        colorData = new byte[ColorTexture.width * ColorTexture.height * 4];

        GameObject.Find( "Color" ).GetComponent<MeshRenderer>().material.mainTexture = ColorTexture;

        DepthTexture = new Texture2D( sensor.DepthFrameSource.FrameDescription.Width, sensor.DepthFrameSource.FrameDescription.Height, TextureFormat.RGBA32, false );
        DepthTexture.hideFlags = HideFlags.HideAndDontSave;
        DepthTexture.Apply();
        depthData = new ushort[DepthTexture.width * DepthTexture.height];
        depthPixelData = new byte[DepthTexture.width * DepthTexture.height * 4];

        GameObject.Find( "Depth" ).GetComponent<MeshRenderer>().material.mainTexture = DepthTexture;

        TrackedColorTexture = new Texture2D( 512, 424, TextureFormat.RGBA32, false );
        TrackedColorTexture.hideFlags = HideFlags.HideAndDontSave;
        TrackedColorTexture.Apply();
        points = new ColorSpacePoint[512 * 424];
        output = new byte[512 * 424 * 4];

        GameObject.Find( "Tracked" ).GetComponent<MeshRenderer>().material.mainTexture = TrackedColorTexture;

        mapper = sensor.CoordinateMapper;
        reader = sensor.OpenMultiSourceFrameReader( FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex );

        reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

        depthThread = new Thread( new ThreadStart( renderDepthFrame ) );
        depthThread.Start();
    }

    Thread depthThread;
    bool runDepthThread = true;
    bool newDepthFrame = false;
    ushort minDepth = 500;
    ushort maxDepth = 4500;
    private void renderDepthFrame() {
        while ( runDepthThread ) {
            if ( newDepthFrame ) {
                //int colorIndex = 0;
                //for ( int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex ) {
                //    ushort depth = depthData[depthIndex];
                //    byte intensity = (byte)( depth >= minDepth && depth <= maxDepth ? depth : (byte)0 );

                //    depthPixelData[colorIndex++] = intensity; // Blue
                //    depthPixelData[colorIndex++] = intensity; // Green
                //    depthPixelData[colorIndex++] = intensity; // Red

                //    ++colorIndex;   
                //}

                //mapper.MapDepthFrameToColorSpace( depthData, points );
                //Array.Clear( output, 0, output.Length );
                //for ( var y = 0; y < 424; y++ ) {
                //    for ( var x = 0; x < 512; x++ ) {
                //        int depthIndex = x + ( y * 512 );
                //        var cPoint = points[depthIndex];
                //        int colorX = (int)Math.Floor( cPoint.X + 0.5 );
                //        int colorY = (int)Math.Floor( cPoint.Y + 0.5 );

                //        if ( ( colorX >= 0 ) && ( colorX < 1920 ) && ( colorY >= 0 ) && ( colorY < 1080 ) ) {
                //            int ci = ( ( colorY * 1920 ) + colorX ) * 4;
                //            int displayIndex = depthIndex * 4;

                //            output[displayIndex + 0] = colorData[ci];
                //            output[displayIndex + 1] = colorData[ci + 1];
                //            output[displayIndex + 2] = colorData[ci + 2];
                //            output[displayIndex + 3] = 0xff;
                //        }

                //    }
                //}

                newDepthFrame = false;
            }
        }
    }

    unsafe void Reader_MultiSourceFrameArrived( object sender, MultiSourceFrameArrivedEventArgs e ) {
        currentFrame++;
        if ( currentFrame % SaveEveryXFrame != 0 ) return;

        var frame = e.FrameReference.AcquireFrame();
        if ( frame == null ) {
            Debug.Log( "Frame expired" );
            return;
        }

        using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() ) {
            colorFrame.CopyConvertedFrameDataToArray( colorData, ColorImageFormat.Rgba );

            ColorTexture.LoadRawTextureData( colorData );
            ColorTexture.Apply();
        }

        using ( var depthFrame = frame.DepthFrameReference.AcquireFrame() ) {
            depthFrame.CopyFrameDataToArray( depthData );

            newDepthFrame = true;

            DepthTexture.LoadRawTextureData( depthPixelData );
            DepthTexture.Apply();

            TrackedColorTexture.LoadRawTextureData( output );
            TrackedColorTexture.Apply();
        }



        using ( var indexFrame = frame.BodyIndexFrameReference.AcquireFrame() ) {
            indexFrame.CopyFrameDataToArray( indexData );

            BodyIndexTexture.LoadRawTextureData( indexData );
            BodyIndexTexture.Apply();
        }

        frame = null;

        if ( Queue != null ) {
            Queue.AddFrame( colorData, depthData, indexData );
        }
    }

    void Update() {
        if ( Input.GetKeyDown( KeyCode.Space ) ) {
            if ( Queue != null ) {
                Queue.SoftStop();
                Queue = null;

                Debug.Log( "Stopping recorder" );
            } else {
                Queue = new SaveQueue( mapper );

                Debug.Log( "started recorder" );
            }


        }
    }

    void OnApplicationQuit() {
        runDepthThread = false;

        if ( Queue != null ) Queue.HardStop();
        Thread.Sleep( 100 );

        reader.Dispose();
        sensor.Close();
    }
}

public abstract class RecordingSaver<T> {
    public Queue<PoolEntry<T>> Frames;

    public int CurrentFrame = 0;

    public static int THREAD_SLEEP_TIME = 10;

    public string CurrentDirectory;

    public bool SoftStop = false;
    public bool HardStop = false;
    public Thread Worker;

    public RecordingSaver( string path, string type ) {
        Frames = new Queue<PoolEntry<T>>();

        CurrentDirectory = Path.Combine( path, type );
        if ( !Directory.Exists( CurrentDirectory ) ) Directory.CreateDirectory( CurrentDirectory );

        Worker = new Thread( new ThreadStart( saveFrames ) );
        Worker.Start();
    }

    public abstract void saveNextFrame();

    private void saveFrames() {
        while ( !HardStop ) {
            if ( Frames.Count == 0 ) {
                if ( SoftStop ) {
                    Debug.LogFormat( "{0}: Done Saving", this.ToString() );
                    break;
                }

                Thread.Sleep( THREAD_SLEEP_TIME );
                continue;
            }

            saveNextFrame();

            CurrentFrame++;
            Thread.Sleep( THREAD_SLEEP_TIME );
        }
    }
}

public class ColorSaver : RecordingSaver<byte> {

    private System.Random rand = new System.Random();

    public ColorSaver( string path ) : base( path, "COLOR" ) {

    }

    public override void saveNextFrame() {
        var frame = Frames.Dequeue();

        var colorFramePath = Path.Combine( CurrentDirectory, CurrentFrame.ToString() + ".uint8" );
        File.WriteAllBytes( colorFramePath, frame.Resource );

        frame.Free();
    }
}

public class DepthSaver : RecordingSaver<ushort> {
    public DepthSaver( string path ) : base( path, "DEPTH" ) { }

    public override void saveNextFrame() {
        var frame = Frames.Dequeue();

        var depthFramePath = Path.Combine( CurrentDirectory, CurrentFrame.ToString() + ".uint16" );
        using ( FileStream fs = new FileStream( depthFramePath, FileMode.CreateNew, FileAccess.Write ) ) {
            using ( BinaryWriter bw = new BinaryWriter( fs ) ) {
                var res = frame.Resource;
                foreach ( short value in res ) {
                    bw.Write( value );
                }
            }
        }

        frame.Free();
    }
}

public class IndexSaver : RecordingSaver<byte> {

    public IndexSaver( string path ) : base( path, "INDEX" ) { }

    public override void saveNextFrame() {
        var frame = Frames.Dequeue();

        var indexFramePath = Path.Combine( CurrentDirectory, CurrentFrame.ToString() + ".uint8" );
        File.WriteAllBytes( indexFramePath, frame.Resource );

        frame.Free();
    }
}

public class TrackedColorSaver : RecordingSaver<ushort> {

    public Queue<PoolEntry<byte>> ColorFrames;

    private CoordinateMapper mapper;

    public static int DEPTH_WIDTH = 512;
    public static int DEPTH_HEIGHT = 424;

    public static int COLOR_WIDTH = 1920;
    public static int COLOR_HEIGHT = 1080;

    private ColorSpacePoint[] points;
    private byte[] output;

    public TrackedColorSaver( string path, CoordinateMapper coordinateMapper ) : base( path, "TRACKEDCOLOR" ) {
        ColorFrames = new Queue<PoolEntry<byte>>();
        mapper = coordinateMapper;

        points = new ColorSpacePoint[DEPTH_WIDTH * DEPTH_HEIGHT];
        output = new byte[DEPTH_WIDTH * DEPTH_HEIGHT * 4];

    }

    public void AddFrame( PoolEntry<ushort> depth, PoolEntry<byte> color ) {
        Frames.Enqueue( depth );
        ColorFrames.Enqueue( color );
    }

    public override void saveNextFrame() {
        var entry = Frames.Dequeue();
        var colorEntry = ColorFrames.Dequeue();
        var colorFrame = colorEntry.Resource;

        mapper.MapDepthFrameToColorSpace( entry.Resource, points );

        Array.Clear( output, 0, output.Length );
        for ( var y = 0; y < DEPTH_HEIGHT; y++ ) {
            for ( var x = 0; x < DEPTH_WIDTH; x++ ) {
                int depthIndex = x + ( y * DEPTH_WIDTH );
                var cPoint = points[depthIndex];
                int colorX = (int)Math.Floor( cPoint.X + 0.5 );
                int colorY = (int)Math.Floor( cPoint.Y + 0.5 );

                if ( ( colorX >= 0 ) && ( colorX < COLOR_WIDTH ) && ( colorY >= 0 ) && ( colorY < COLOR_HEIGHT ) ) {
                    int colorIndex = ( ( colorY * COLOR_WIDTH ) + colorX ) * 4;
                    int displayIndex = depthIndex * 4;

                    output[displayIndex + 0] = colorFrame[colorIndex];
                    output[displayIndex + 1] = colorFrame[colorIndex + 1];
                    output[displayIndex + 2] = colorFrame[colorIndex + 2];
                    output[displayIndex + 3] = 0xff;
                }

            }
        }

        var trackedFramePath = Path.Combine( CurrentDirectory, CurrentFrame.ToString() + ".uint8" );
        File.WriteAllBytes( trackedFramePath, output );

        entry.Free();
        colorEntry.Free();
    }
}

public class SaveQueue {
    public static string BASE_PATH = @"C:\Recordings";

    public string CurrentPath;

    private ColorSaver color;
    private DepthSaver depth;
    private IndexSaver index;
    private TrackedColorSaver tracked;

    private ArrayPool<byte> colorPool;
    private ArrayPool<ushort> depthPool;
    private ArrayPool<byte> indexPool;

    public SaveQueue( CoordinateMapper mapper ) {
        if ( !Directory.Exists( BASE_PATH ) ) Directory.CreateDirectory( BASE_PATH );
        CurrentPath = Path.Combine( BASE_PATH, DateTime.Now.ToString().Replace( ':', '_' ).Replace( '/', '_' ) );
        Directory.CreateDirectory( CurrentPath );

        color = new ColorSaver( CurrentPath );
        depth = new DepthSaver( CurrentPath );
        index = new IndexSaver( CurrentPath );
        tracked = new TrackedColorSaver( CurrentPath, mapper );

        colorPool = new ArrayPool<byte>( 1920 * 1080 * 4, 10 );
        depthPool = new ArrayPool<ushort>( 512 * 424, 10 );
        indexPool = new ArrayPool<byte>( 512 * 424, 10 );
    }

    public void AddFrame( byte[] colorData, ushort[] depthData, byte[] indexData ) {
        var _color = colorPool.RequestResource( 2 );
        colorData.CopyTo( _color.Resource, 0 );
        color.Frames.Enqueue( _color );

        var _depth = depthPool.RequestResource( 2 );
        depthData.CopyTo( _depth.Resource, 0 );
        depth.Frames.Enqueue( _depth );

        var _index = indexPool.RequestResource( 1 );                
        indexData.CopyTo( _index.Resource, 0 );
        index.Frames.Enqueue( _index );

        tracked.AddFrame( _depth, _color );
    }

    public void SoftStop() {
        color.SoftStop = depth.SoftStop = index.SoftStop = tracked.SoftStop = true;
    }

    public void HardStop() {
        color.HardStop = depth.HardStop = index.HardStop = tracked.HardStop = true;
    }
}