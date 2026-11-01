using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class CoinSnatcher : MonoBehaviour
{
    public RectTransform playfield, catcher, drops;
    public TMP_Text scoreLabel, bestLabel, messageLabel, resultLabel, actionLabel;
    public GameObject menu;
    public UnityEngine.UI.Button actionButton;
    public Sprite circle;
    public TMP_FontAsset font;
    public int Score { get; private set; }
    public bool IsPlaying { get; private set; }
    public int CoinsCaught { get; private set; }
    public int BombsHit { get; private set; }
    public int ActiveDrops => items.Count;
    static readonly Color Ink = Hex("101A20"), Muted = Hex("809499"), White = Hex("EAF4EE"), Gold = Hex("FFD16A"), Red = Hex("FF7975"), Mint = Hex("A8E2D1");
    class Drop { public RectTransform rect; public bool bomb; public float speed; }
    readonly List<Drop> items = new List<Drop>();
    float elapsed, nextSpawn;
    int best;
    bool paused;

    void Awake()
    {
        best = PlayerPrefs.GetInt("CoinSnatcher.Best", 0);
        actionButton.onClick.AddListener(StartRun);
        UpdateLabels();
    }
    void Update()
    {
        var key = Keyboard.current;
        if (key != null && (key.enterKey.wasPressedThisFrame || key.spaceKey.wasPressedThisFrame) && !IsPlaying) StartRun();
        if (key != null && key.escapeKey.wasPressedThisFrame && IsPlaying) { paused = !paused; messageLabel.text = paused ? "PAUSED  /  ESC TO RESUME" : ""; }
        if (paused) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        if (!IsPlaying) return;
        float move = 0;
        if (key != null) {
            if (key.aKey.isPressed || key.leftArrowKey.isPressed) move -= 1;
            if (key.dKey.isPressed || key.rightArrowKey.isPressed) move += 1;
        }
        var position = catcher.anchoredPosition;
        position.x += move * 760 * dt;
        Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        bool dragging = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) {
            pointer = Touchscreen.current.primaryTouch.position.ReadValue(); dragging = true;
        }
        if (dragging && RectTransformUtility.ScreenPointToLocalPointInRectangle(playfield, pointer, null, out var local)) position.x = local.x;
        position.x = Mathf.Clamp(position.x, -450, 450);
        catcher.anchoredPosition = position;
        elapsed += dt; nextSpawn -= dt;
        if (nextSpawn <= 0) {
            Spawn(Random.value < (elapsed < 2 ? 0 : .23f), Random.Range(-440f, 440f));
            nextSpawn = Mathf.Max(.28f, .72f - elapsed * .004f);
        }
        for (int i = items.Count - 1; i >= 0; i--) {
            var item = items[i]; var p = item.rect.anchoredPosition; float oldY = p.y;
            p.y -= item.speed * dt; item.rect.anchoredPosition = p;
            // Sweep across the catch line: even a fast drop cannot skip a collision.
            if (oldY >= -213 && p.y <= -213 && Mathf.Abs(p.x - position.x) < 68) {
                if (item.bomb) { BombsHit++; EndRun(); break; }
                CoinsCaught++; Score++; UpdateLabels();
                Destroy(item.rect.gameObject); items.RemoveAt(i);
            } else if (p.y < -290) { Destroy(item.rect.gameObject); items.RemoveAt(i); }
        }
    }
    public void StartRun()
    {
        ClearDrops(); Score = 0; CoinsCaught = 0; BombsHit = 0; elapsed = 0; nextSpawn = .2f; paused = false;
        catcher.anchoredPosition = new Vector2(0, -235); IsPlaying = true; menu.SetActive(false); messageLabel.text = ""; UpdateLabels();
    }
    void EndRun()
    {
        IsPlaying = false;
        if (Score > best) { best = Score; PlayerPrefs.SetInt("CoinSnatcher.Best", best); PlayerPrefs.Save(); }
        messageLabel.text = ""; resultLabel.text = "BOOM. ONE MORE?";
        menu.transform.Find("MenuSubtitle").GetComponent<TMP_Text>().text = "You snatched " + Score + (Score == 1 ? " coin." : " coins.") + "  Let's beat that.";
        actionLabel.text = "PLAY AGAIN   >"; menu.SetActive(true); UpdateLabels();
    }
    void UpdateLabels() { scoreLabel.text = Score.ToString("00"); bestLabel.text = "BEST  " + best.ToString("00"); }
    void ClearDrops() { foreach (var i in items) if (i.rect) Destroy(i.rect.gameObject); items.Clear(); }
    public void Spawn(bool bomb, float x)
    {
        var r = MakeIcon(bomb ? "Bomb" : "Coin", drops, new Vector2(x, 255), bomb);
        items.Add(new Drop { rect = r, bomb = bomb, speed = Random.Range(190, 250) + Mathf.Min(180, elapsed * 2.5f) });
    }
    public void BuildLayout(Sprite disc, TMP_FontAsset typeface)
    {
        circle = disc; font = typeface;
        var canvasObj = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,800); scaler.matchWidthOrHeight = .5f;
        var canvas = canvasObj.GetComponent<RectTransform>();
        var background = Box("Background",canvas,Vector2.zero,new Vector2(4000,4000),Ink);
        Text("Edition", canvas, "01  /  POCKET ARCADE", new Vector2(-440,342),new Vector2(280,28),14,Muted, TextAlignmentOptions.Left);
        Text("Title",canvas,"COIN SNATCHER",new Vector2(-395,296),new Vector2(370,52),32,White,TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;
        bestLabel = Text("Best",canvas,"BEST  00",new Vector2(450,309),new Vector2(200,44),20,Mint,TextAlignmentOptions.Right);
        Box("Header Rule",canvas,new Vector2(0,263),new Vector2(1120,1),Hex("314148"));
        playfield = new GameObject("Playfield", typeof(RectTransform)).GetComponent<RectTransform>(); playfield.SetParent(canvas,false); playfield.sizeDelta = new Vector2(1020,520); playfield.anchoredPosition = new Vector2(0,-12);
        Box("Left Edge",playfield,new Vector2(-510,0),new Vector2(1,500),Hex("24343B"));
        Box("Right Edge",playfield,new Vector2(510,0),new Vector2(1,500),Hex("24343B"));
        for(int i=0;i<7;i++) Box("Floor Mark",playfield,new Vector2(-420+i*140,-268),new Vector2(22,2),Hex("314148"));
        scoreLabel = Text("Score",playfield,"00",new Vector2(0,178),new Vector2(240,110),84,Hex("26383F")); scoreLabel.fontStyle = FontStyles.Bold;
        Text("Score Caption",playfield,"COINS SNATCHED",new Vector2(0,119),new Vector2(250,26),12,Muted);
        drops = new GameObject("Falling Objects",typeof(RectTransform)).GetComponent<RectTransform>(); drops.SetParent(playfield,false); drops.sizeDelta = playfield.sizeDelta;
        catcher = Box("Catcher",playfield,new Vector2(0,-235),new Vector2(110,20),Mint);
        Box("Left Lip",catcher,new Vector2(-49,15),new Vector2(12,24),Mint);
        Box("Right Lip",catcher,new Vector2(49,15),new Vector2(12,24),Mint);
        Box("Catcher Detail",catcher,new Vector2(0,-1),new Vector2(30,3),Hex("689F90"));
        messageLabel = Text("Message",playfield,"",new Vector2(0,-150),new Vector2(700,35),18,Gold);
        menu = Box("Menu",playfield,new Vector2(0,-15),new Vector2(680,260),Ink).gameObject;
        MakeIcon("Coin Example",menu.transform,new Vector2(-35,80),false);
        MakeIcon("Bomb Example",menu.transform,new Vector2(35,80),true);
        resultLabel = Text("MenuTitle",menu.transform,"CATCH A LITTLE LUCK.",new Vector2(0,17),new Vector2(640,60),34,White); resultLabel.fontStyle = FontStyles.Bold;
        Text("MenuSubtitle",menu.transform,"Snatch the gold. Dodge the bombs.",new Vector2(0,-32),new Vector2(640,38),19,Muted);
        var button = Box("Play Button",menu.transform,new Vector2(0,-101),new Vector2(230,55),Gold);
        button.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        actionButton = button.gameObject.AddComponent<UnityEngine.UI.Button>(); actionButton.targetGraphic = button.GetComponent<UnityEngine.UI.Image>();
        var colors = actionButton.colors; colors.highlightedColor = new Color(1,.93f,.75f); colors.pressedColor = new Color(.8f,.7f,.5f); actionButton.colors = colors;
        actionLabel = Text("Action",button,"LET'S PLAY   >",Vector2.zero,new Vector2(220,50),17,Ink); actionLabel.fontStyle = FontStyles.Bold;
        Box("Footer Rule",canvas,new Vector2(0,-310),new Vector2(1120,1),Hex("314148"));
        Text("Controls",canvas,"A / D  or  ARROWS     /     DRAG TO MOVE",new Vector2(-225,-345),new Vector2(680,30),15,Muted,TextAlignmentOptions.Left);
        Text("Rules",canvas,"GOLD +1     /     BOMB = GAME OVER",new Vector2(367,-345),new Vector2(400,30),13,Muted,TextAlignmentOptions.Right);
        if (Object.FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
    }
    RectTransform MakeIcon(string name, Transform parent, Vector2 at, bool bomb)
    {
        var r = Box(name,parent,at,Vector2.one * 36,bomb ? Red : Gold,circle);
        if(bomb) {
            var fuse = Box("Fuse",r,new Vector2(11,20),new Vector2(4,12),White); fuse.localRotation = Quaternion.Euler(0,0,-30);
            Box("Fuse Spark",r,new Vector2(15,27),Vector2.one * 6,Gold,circle);
            Box("Shine",r,new Vector2(-6,6),Vector2.one * 7,Hex("FFB6A2"),circle);
        } else {
            Box("Inset",r,Vector2.zero,Vector2.one*26,Hex("C28D34"),circle);
            Box("Face",r,Vector2.zero,Vector2.one*22,Gold,circle);
            Box("Stamp",r,Vector2.zero,new Vector2(3,12),Hex("A97628"));
        }
        return r;
    }
    RectTransform Box(string name,Transform parent,Vector2 pos,Vector2 size,Color color,Sprite sprite = null)
    {
        var g=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); g.transform.SetParent(parent,false);
        var r=g.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=pos; r.sizeDelta=size;
        var image=g.GetComponent<UnityEngine.UI.Image>(); image.color=color; image.sprite=sprite; image.raycastTarget=false; return r;
    }
    TMP_Text Text(string name,Transform parent,string content,Vector2 pos,Vector2 size,float fontSize,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
    {
        var g=new GameObject(name,typeof(RectTransform)); g.transform.SetParent(parent,false);
        var r=g.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=pos; r.sizeDelta=size;
        var text=g.AddComponent<TextMeshProUGUI>(); text.font=font; text.text=content; text.fontSize=fontSize; text.color=color; text.alignment=alignment; text.raycastTarget=false; return text;
    }
    static Color Hex(string s) { ColorUtility.TryParseHtmlString("#"+s,out var c); return c; }
}
