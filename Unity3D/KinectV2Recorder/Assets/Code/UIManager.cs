using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    [HideInInspector]
    public Recorder Recorder;

    public GameObject ToggleGroup;

    public InputField FramesPerSecondInput;

    public InputField PathInput;
    public Button RecordButton;
    [HideInInspector]
    public Text RecordButtonText;

    public bool IsRecording { get { return Recorder.Queue != null; } }

    public SaveQueue CurrentQueue;

    void Start() {
        Recorder = FindObjectOfType<Recorder>();
        RecordButtonText = RecordButton.GetComponentInChildren<Text>();
    }

    void Update() {
        var fps = string.IsNullOrEmpty( FramesPerSecondInput.text ) ? 0 : int.Parse( FramesPerSecondInput.text );
        fps = Mathf.Clamp( fps, 0, 30 );
        Recorder.SaveXFramesPerSecond = fps;

        if( RecordButtonText.text == "Saving..." && CurrentQueue.FinishedSaving()) {
            RecordButtonText.text = "Record";
            RecordButton.enabled = true;            
        }
    }

    public void OnRecordButtonClick() {
        if ( !IsRecording ) {
            RecordButtonText.text = "Stop recording";
            if ( Directory.Exists( PathInput.text ) ) {
                CurrentQueue = Recorder.StartRecording( PathInput.text );
            } else {
                Debug.LogErrorFormat( "{0} is not a valid directory", PathInput.text );
            }            
        } else {
            RecordButtonText.text = "Saving...";
            RecordButton.enabled = false;
            Recorder.StopRecording();
        }
    }

    public void ColorToggleChanged() {
        Recorder.ToggleColorStream();
    }

    public void DepthToggleChanged() {
        Recorder.ToggleDepthStream();
    }

    public void BodyIndexToggleChanged() {
        Recorder.ToggleIndexStream();
    }

    public void ColorOnDepthToggleChanged() {
        Recorder.ToggleColorOnDepthStream();
    }
}
