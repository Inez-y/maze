using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Console : MonoBehaviour
{
    bool listening = false;
    bool consoleOn = false;

    public Paddle p0;
    public Paddle p1;
    public Ball b;
    public PlayerInput pi0;
    public PlayerInput pi1;
    public GameObject panel;
    public TMP_InputField inf;
    public GameObject backWall;

    void Start()
    {
        EventManager.GameStart += (PaddleType pt) => { listening = true; };
        inf.onSubmit.AddListener(HandleSubmit);
        inf.onEndEdit.AddListener(HandleSubmit);
    }

    void Update()
    {
        if (!listening) return;

        if (Input.GetKeyDown(KeyCode.C) && !consoleOn)
            ToggleConsole(true);
        else if (Input.GetKeyDown(KeyCode.Escape) && consoleOn)
            ToggleConsole(false);
    }

    void ToggleConsole(bool state)
    {
        consoleOn = state;

        Time.timeScale = consoleOn ? 0f : 1f;
        pi0.enabled = !consoleOn;
        pi1.enabled = !consoleOn;
        panel.SetActive(consoleOn);

        if (consoleOn)
        {
            inf.text = "";
            inf.ActivateInputField();
        }
        else
        {
            inf.DeactivateInputField();
        }
    }

    void HandleSubmit(string text)
    {
        if (!consoleOn || string.IsNullOrWhiteSpace(text)) return;

        ExecuteCommand(text.Trim());
        inf.text = "";
        inf.ActivateInputField();
    }

    void ExecuteCommand(string line)
    {
        string[] parts = line.Split(' ');
        string cmd = parts[0].ToLower();

        if (cmd == "wall")
        {
            Renderer r = backWall.GetComponent<Renderer>();
            Color c;

            if (parts.Length == 2)
                ColorUtility.TryParseHtmlString(parts[1], out c);
            else
                c = new Color(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));

            r.material.color = c;
        }
        else if (cmd == "boost")
        {
            float m = float.Parse(parts[1]);
            b.acceleration *= m;
            p0.speed *= m;
            p1.speed *= m;
        }
    }
}
