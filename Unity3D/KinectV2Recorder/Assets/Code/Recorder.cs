using UnityEngine;
using System.Collections;
using Windows.Kinect;

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

    public static int INDEX_WIDTH = 512;
    public static int INDEX_HEIGHT = 424;
    public static int INDEX_LENGTH = INDEX_WIDTH * INDEX_HEIGHT;

    void Start () {
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

        mapper = sensor.CoordinateMapper;        
        reader = sensor.OpenMultiSourceFrameReader( FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex );
        
        reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

    }

    private void Reader_MultiSourceFrameArrived( object sender, MultiSourceFrameArrivedEventArgs e ) {
        var frame = e.FrameReference.AcquireFrame();
        if( frame == null ) {
            Debug.Log( "Frame expired" );
            return;
        }

        using ( var depthFrame = frame.DepthFrameReference.AcquireFrame() ) {
            depthFrame.CopyFrameDataToArray( depthData );

            ushort minDepth = depthFrame.DepthMinReliableDistance;
            ushort maxDepth = depthFrame.DepthMaxReliableDistance;

            int colorIndex = 0;
            for ( int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex ) {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte)( depth >= minDepth && depth <= maxDepth ? depth : (byte)0 );

                depthPixelData[colorIndex++] = intensity; // Blue
                depthPixelData[colorIndex++] = intensity; // Green
                depthPixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }
            
            DepthTexture.LoadRawTextureData( depthPixelData );
            DepthTexture.Apply();

        }
        
        using ( var colorFrame = frame.ColorFrameReference.AcquireFrame() ) {
            colorFrame.CopyConvertedFrameDataToArray( colorData, ColorImageFormat.Rgba );
            ColorTexture.LoadRawTextureData( colorData );
            ColorTexture.Apply();            
        }

        using ( var indexFrame = frame.BodyIndexFrameReference.AcquireFrame() ) {
            indexFrame.CopyFrameDataToArray( indexData );

            BodyIndexTexture.LoadRawTextureData( indexData );
            BodyIndexTexture.Apply();
        }

        frame = null;
    }

    void Update () {
	
	}

    void OnApplicationQuit() {
        reader.Dispose();
        sensor.Close();
    }
}
