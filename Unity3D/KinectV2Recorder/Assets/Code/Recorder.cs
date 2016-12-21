using UnityEngine;
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

    public static int COLOR_WIDTH = 1920;
    public static int COLOR_HEIGHT = 1080;
    public static int COLOR_LENGTH = COLOR_WIDTH * COLOR_HEIGHT;
    public static int COLOR_SIZE = COLOR_LENGTH * 4;

    public static int DEPTH_WIDTH = 512;
    public static int DEPTH_HEIGHT = 424;
    public static int DEPTH_LENGTH = DEPTH_HEIGHT * DEPTH_WIDTH;

    public SaveQueue Queue;

    private int currentFrame = 0;

    public int SaveXFramesPerSecond = 10;
    private int saveEveryXFrame { get { return 30 / SaveXFramesPerSecond; } }

    public GameObject ColorPlane;
    public GameObject DepthPlane;
    public GameObject IndexPlane;
    public GameObject ColorOnDepthPlane;
    private bool recordColorFrame = true;
    private bool recordDepthFrame = true;
    private bool recordIndexFrame = true;
    private bool recordColorOnDepthFrame = true;

    void Start() {
        ColorPlane = GameObject.Find( "Color" );
        DepthPlane = GameObject.Find( "Depth" );
        IndexPlane = GameObject.Find( "BodyIndex" );
        ColorOnDepthPlane = GameObject.Find( "ColorOnDepth" );

        sensor = KinectSensor.GetDefault();
        sensor.Open();

        mapper = sensor.CoordinateMapper;

        reader = sensor.OpenMultiSourceFrameReader( FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex );
        reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;


        ColorTexture = new Texture2D( sensor.ColorFrameSource.FrameDescription.Width, sensor.ColorFrameSource.FrameDescription.Height, TextureFormat.RGBA32, false );
        ColorTexture.hideFlags = HideFlags.HideAndDontSave;
        ColorTexture.Apply();
        colorData = new byte[ColorTexture.width * ColorTexture.height * 4];

        ColorPlane.GetComponent<MeshRenderer>().material.mainTexture = ColorTexture;

        DepthTexture = new Texture2D( sensor.DepthFrameSource.FrameDescription.Width, sensor.DepthFrameSource.FrameDescription.Height, TextureFormat.RGBA32, false );
        DepthTexture.hideFlags = HideFlags.HideAndDontSave;
        DepthTexture.Apply();
        depthData = new ushort[DepthTexture.width * DepthTexture.height];
        depthPixelData = new byte[DepthTexture.width * DepthTexture.height * 4];

        DepthPlane.GetComponent<MeshRenderer>().material.mainTexture = DepthTexture;

        BodyIndexTexture = new Texture2D( INDEX_WIDTH, INDEX_HEIGHT, TextureFormat.Alpha8, false );
        BodyIndexTexture.hideFlags = HideFlags.HideAndDontSave;
        BodyIndexTexture.Apply();
        indexData = new byte[INDEX_WIDTH * INDEX_HEIGHT];
        IndexPlane.GetComponent<MeshRenderer>().material.mainTexture = BodyIndexTexture;

        TrackedColorTexture = new Texture2D( DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RGBA32, false );
        TrackedColorTexture.hideFlags = HideFlags.HideAndDontSave;
        TrackedColorTexture.Apply();
        points = new ColorSpacePoint[DEPTH_LENGTH];
        output = new byte[DEPTH_LENGTH * 4];

        ColorOnDepthPlane.GetComponent<MeshRenderer>().material.mainTexture = TrackedColorTexture;


        depthThread = new Thread( new ThreadStart( renderDepthFrame ) );
        depthThread.Start();

        colorOnDepthThread = new Thread( new ThreadStart( renderColorOnDepthThread ) );
        colorOnDepthThread.Start();
    }

    Thread depthThread;
    bool runDepthThread = true;
    bool newDepthFrame = false;
    ushort minDepth = 500;
    ushort maxDepth = 4500;
    private void renderDepthFrame() {
        while ( runDepthThread ) {
            if ( recordDepthFrame && newDepthFrame ) {
                newDepthFrame = false;

                int colorIndex = 0;
                for ( int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex ) {
                    ushort depth = depthData[depthIndex];
                    byte intensity = (byte)( depth >= minDepth && depth <= maxDepth ? depth : (byte)0 );

                    depthPixelData[colorIndex++] = intensity; // Blue
                    depthPixelData[colorIndex++] = intensity; // Green
                    depthPixelData[colorIndex++] = intensity; // Red

                    ++colorIndex;
                }                
            }

            Thread.Sleep( 1 );
        }
    }

    Thread colorOnDepthThread;
    bool runColorOnDepthThread = true;
    bool newColorOnDepthFrame = false;
    private void renderColorOnDepthThread() {
        while ( runColorOnDepthThread ) {
            if ( newColorOnDepthFrame && recordColorOnDepthFrame ) {
                newColorOnDepthFrame = false;

                mapper.MapDepthFrameToColorSpace( depthData, points );
                Array.Clear( output, 0, output.Length );
                for ( var y = 0; y < DEPTH_HEIGHT; y++ ) {
                    for ( var x = 0; x < DEPTH_WIDTH; x++ ) {
                        int depthIndex = x + ( y * DEPTH_WIDTH );
                        var cPoint = points[depthIndex];
                        int colorX = (int)Math.Floor( cPoint.X + 0.5 );
                        int colorY = (int)Math.Floor( cPoint.Y + 0.5 );

                        if ( ( colorX >= 0 ) && ( colorX < COLOR_WIDTH ) && ( colorY >= 0 ) && ( colorY < COLOR_HEIGHT ) ) {
                            int ci = ( ( colorY * COLOR_WIDTH ) + colorX ) * 4;
                            int displayIndex = depthIndex * 4;

                            output[displayIndex + 0] = colorData[ci];
                            output[displayIndex + 1] = colorData[ci + 1];
                            output[displayIndex + 2] = colorData[ci + 2];
                            output[displayIndex + 3] = 0xff;
                        }
                    }
                }
            }

            Thread.Sleep( 1 );
        }
    }

    public void ToggleColorStream() {
        recordColorFrame = !recordColorFrame;

        ColorPlane.SetActive( recordColorFrame );
    }

    public void ToggleDepthStream() {
        recordDepthFrame = !recordDepthFrame;

        DepthPlane.SetActive( recordDepthFrame );
    }

    public void ToggleIndexStream() {
        recordIndexFrame = !recordIndexFrame;

        IndexPlane.SetActive( recordIndexFrame );
    }

    public void ToggleColorOnDepthStream() {
        recordColorOnDepthFrame = !recordColorOnDepthFrame;

        ColorOnDepthPlane.SetActive( recordColorOnDepthFrame );
    }

    unsafe void Reader_MultiSourceFrameArrived( object sender, MultiSourceFrameArrivedEventArgs e ) {
        currentFrame++;
        if ( currentFrame % saveEveryXFrame != 0 ) return;

        var frame = e.FrameReference.AcquireFrame();
        if ( frame == null ) return;

        if ( recordColorFrame ) {
            using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() ) {
                colorFrame.CopyConvertedFrameDataToArray( colorData, ColorImageFormat.Rgba );

                ColorTexture.LoadRawTextureData( colorData );
                ColorTexture.Apply();
            }
        }

        if ( recordDepthFrame || recordColorOnDepthFrame ) {
            using ( var depthFrame = frame.DepthFrameReference.AcquireFrame() ) {
                depthFrame.CopyFrameDataToArray( depthData );

                if ( recordDepthFrame ) {
                    DepthTexture.LoadRawTextureData( depthPixelData );
                    DepthTexture.Apply();
                }

                if ( recordColorOnDepthFrame ) {
                    TrackedColorTexture.LoadRawTextureData( output );
                    TrackedColorTexture.Apply();
                }

                newDepthFrame = true;

                newColorOnDepthFrame = true;
            }
        }

        if ( recordIndexFrame ) {
            using ( var indexFrame = frame.BodyIndexFrameReference.AcquireFrame() ) {
                indexFrame.CopyFrameDataToArray( indexData );

                BodyIndexTexture.LoadRawTextureData( indexData );
                BodyIndexTexture.Apply();
            }
        }

        frame = null;

        if ( Queue != null ) {
            var color = recordColorFrame ? colorData : null;
            var depth = recordDepthFrame || recordColorOnDepthFrame ? depthData : null;
            var index = recordIndexFrame ? indexData : null;

            Queue.AddFrame( color, depth, index );
        }
    }

    void Update() {
    }

    public void StartRecording( string path ) {
        if ( Queue != null ) {
            Debug.LogError( "Already recording!" );
            return;
        }

        Queue = new SaveQueue( path, mapper, recordColorFrame, recordDepthFrame, recordIndexFrame, recordColorOnDepthFrame );
    }

    public void StopRecording() {
        if ( Queue == null ) {
            Debug.LogError( "Already stopped!" );
            return;
        }

        Queue.SoftStop();
        Queue = null;
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

        Frames.Clear();
    }
}

public class ColorSaver : RecordingSaver<byte> {

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

    private ColorSpacePoint[] points;
    private byte[] output;

    public TrackedColorSaver( string path, CoordinateMapper coordinateMapper ) : base( path, "TRACKEDCOLOR" ) {
        ColorFrames = new Queue<PoolEntry<byte>>();
        mapper = coordinateMapper;

        points = new ColorSpacePoint[Recorder.DEPTH_LENGTH];
        output = new byte[Recorder.DEPTH_LENGTH * 4];
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
        for ( var y = 0; y < Recorder.DEPTH_HEIGHT; y++ ) {
            for ( var x = 0; x < Recorder.DEPTH_WIDTH; x++ ) {
                int depthIndex = x + ( y * Recorder.DEPTH_WIDTH );
                var cPoint = points[depthIndex];
                int colorX = (int)Math.Floor( cPoint.X + 0.5 );
                int colorY = (int)Math.Floor( cPoint.Y + 0.5 );

                if ( ( colorX >= 0 ) && ( colorX < Recorder.COLOR_WIDTH ) && ( colorY >= 0 ) && ( colorY < Recorder.COLOR_HEIGHT ) ) {
                    int colorIndex = ( ( colorY * Recorder.COLOR_WIDTH ) + colorX ) * 4;
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

[Flags]
public enum SaveOutputs {
    Color,
    Depth,
    Index,
    TrackedColor
}

public class SaveQueue {
    public string BasePath = @"C:\Recordings";

    public string CurrentPath;

    private ColorSaver color;
    private DepthSaver depth;
    private IndexSaver index;
    private TrackedColorSaver tracked;

    private ArrayPool<byte> colorPool;
    private ArrayPool<ushort> depthPool;
    private ArrayPool<byte> indexPool;

    public bool SaveColor = true;
    public bool SaveDepth = true;
    public bool SaveIndex = true;
    public bool SaveTracked = true;

    public const int COLOR_POOL_SIZE = 30;
    public const int DEPTH_POOL_SIZE = 10;
    public const int INDEX_POOL_SIZE = 10;

    //TODO configure which ones you want to have in save queue
    public SaveQueue( string path, CoordinateMapper mapper, bool saveColor, bool saveDepth, bool saveIndex, bool saveTracked ) {
        SaveColor = saveColor;
        SaveDepth = saveDepth;
        SaveIndex = saveIndex;
        SaveTracked = saveTracked;

        if ( !String.IsNullOrEmpty( path ) ) {
            BasePath = path;
        }

        if ( !Directory.Exists( BasePath ) ) Directory.CreateDirectory( BasePath );
        CurrentPath = Path.Combine( BasePath, DateTime.Now.ToString().Replace( ':', '_' ).Replace( '/', '_' ) );
        Directory.CreateDirectory( CurrentPath );

        
        if(SaveColor || SaveTracked) {
            if(SaveColor) {
                color = new ColorSaver( CurrentPath );
            }
            
            colorPool = new ArrayPool<byte>( Recorder.COLOR_SIZE, COLOR_POOL_SIZE );
        }
        
        if(SaveDepth || SaveTracked) {
            if(SaveDepth) {
                depth = new DepthSaver( CurrentPath );
            }
            
            depthPool = new ArrayPool<ushort>( Recorder.DEPTH_LENGTH, DEPTH_POOL_SIZE );
        }
        
        if(SaveIndex) {
            index = new IndexSaver( CurrentPath );
            indexPool = new ArrayPool<byte>( Recorder.INDEX_LENGTH, INDEX_POOL_SIZE );
        }
        
        if(SaveTracked) {
            tracked = new TrackedColorSaver( CurrentPath, mapper );
        }                              
    }

    public void AddFrame( byte[] colorData, ushort[] depthData, byte[] indexData ) {

        var colorCount = Convert.ToInt32( SaveColor ) + Convert.ToInt32( SaveTracked );
        var depthCount = Convert.ToInt32( SaveDepth ) + Convert.ToInt32( SaveTracked );

        PoolEntry<byte> _color = null;
        PoolEntry<ushort> _depth = null;

        if(SaveColor || SaveTracked) {
            _color = colorPool.RequestResource( colorCount );
            colorData.CopyTo( _color.Resource, 0 );

            if(SaveColor) {
                color.Frames.Enqueue( _color );
            }
        }

        
        if(SaveDepth || SaveTracked) {
            _depth = depthPool.RequestResource( depthCount );
            depthData.CopyTo( _depth.Resource, 0 );
            
            if(SaveDepth) {
                depth.Frames.Enqueue( _depth );
            }
        }

        if(SaveTracked) {
            tracked.AddFrame( _depth, _color );
        }

        if(SaveIndex) {
            var _index = indexPool.RequestResource( 1 );
            indexData.CopyTo( _index.Resource, 0 );
            index.Frames.Enqueue( _index );
        }        
    }

    public void SoftStop() {
        if ( colorPool != null ) colorPool.Dispose();
        if ( color != null ) color.SoftStop = true;

        if ( depthPool != null ) depthPool.Dispose();
        if ( depth != null ) depth.SoftStop = true;

        if ( indexPool != null ) indexPool.Dispose();
        if ( index != null ) index.SoftStop = true;

        if ( tracked != null ) tracked.SoftStop = true;

        GC.Collect();
    }

    public void HardStop() {
        if ( colorPool != null ) colorPool.Dispose();
        if ( color != null ) color.HardStop = true;

        if ( depthPool != null ) depthPool.Dispose();
        if ( depth != null ) depth.HardStop = true;

        if ( indexPool != null ) indexPool.Dispose();
        if ( index != null ) index.HardStop = true;

        if ( tracked != null ) tracked.HardStop = true;

        GC.Collect();
    }
}