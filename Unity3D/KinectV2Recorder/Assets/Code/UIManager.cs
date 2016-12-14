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

    void Start() {
        Recorder = FindObjectOfType<Recorder>();
        RecordButtonText = RecordButton.GetComponentInChildren<Text>();
    }

    void Update() {

    }



    public void OnRecordButtonClick() {
        if ( !IsRecording ) {
            RecordButtonText.text = "Stop recording";
            if ( Directory.Exists( PathInput.text ) ) {
                Recorder.StartRecording( PathInput.text );
            } else {
                Debug.LogErrorFormat( "{0} is not a valid directory", PathInput.text );
            }            
        } else {
            RecordButtonText.text = "Saving...";
        }
    }

    public void ColorToggleChanged() {

    }

    public void DepthToggleChanged() {

    }

    public void BodyIndexToggleChanged() {

    }

    public void ColorOnDepthToggleChanged() {

    }
}
