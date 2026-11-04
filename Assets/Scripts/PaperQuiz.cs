using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class QuizQuestion
{
    public string id;
    [TextArea(1, 3)] public string text;
    public string[] options;
}

/// <summary>
/// The questionnaire, drawn as an open book (the book from the table): a red cover, stacks of
/// page edges, a ruled left page with notes, and the questions handwritten on the right page.
/// The ray highlights the answer it points at in yellow; the chosen answer gets a blue pen cross.
/// One question per page, up to 5 options.
///
/// Built in code, driven by EndingManager. All textures are generated here, nothing to import.
/// The handwriting font is optional (a TTF imported as a Font); without it the default font is used.
/// Sizes are in metres (3D TextMeshPro: font size 1 is a text line about 11 cm tall).
///
/// Draw order: every flat piece gets its own render queue (3010 and up, text at 3100). This keeps the
/// text above the paper whatever the camera angle, and the whole book above the wall signs, which
/// are drawn at 3000 (transparent objects are otherwise sorted by distance).
/// </summary>
public class PaperQuiz : MonoBehaviour
{
    private static readonly Color InkColor = new Color(0.10f, 0.10f, 0.14f);
    private static readonly Color GreyInk = new Color(0.38f, 0.38f, 0.42f);
    private static readonly Color PencilInk = new Color(0.45f, 0.45f, 0.50f);
    private static readonly Color PenInk = new Color(0.10f, 0.20f, 0.70f);
    private static readonly Color HighlightColor = new Color(1f, 0.92f, 0.35f, 0.55f);
    private static readonly Color CoverColor = new Color(0.45f, 0.08f, 0.10f);

    private const float PageWidth = 0.85f;
    private const float PageHeight = 0.85f;
    private const float RowHeight = 0.11f;
    private const float RowStep = 0.12f;

    private Transform root;       // origin = centre of the RIGHT page (where the questions are)
    private Transform page;
    private Transform viewer;
    private TMP_FontAsset font;
    private bool flipFacing;
    private float side;           // +1 when local +x is the participant's right, -1 otherwise
    private float behind;         // +1 / -1: which local z side is "behind" the text
    private int choice = -1;
    private Texture2D rightTexture;
    private Texture2D leftTexture;
    private GameObject notes;

    public void Init(Transform viewerTransform, bool flip, Font handwriting)
    {
        viewer = viewerTransform;
        flipFacing = flip;
        side = flip ? 1f : -1f;
        behind = flip ? 1f : -1f;

        if (handwriting != null) font = TMP_FontAsset.CreateFontAsset(handwriting);

        root = new GameObject("PaperQuiz").transform;
        root.SetParent(transform, false);

        BuildBook();
        root.gameObject.SetActive(false);
    }

    // ---- the book -------------------------------------------------------------------------

    private void BuildBook()
    {
        rightTexture = MakePaperTexture(false);
        leftTexture = MakePaperTexture(true);
        float leftX = -(PageWidth + 0.01f); // viewer-right offset of the left page centre
        float spineX = -(PageWidth + 0.01f) / 2f;

        // soft shadow under the whole book
        AddFlat("BookShadow", spineX, -0.035f, PageWidth * 2f + 0.16f, PageHeight + 0.14f,
                new Color(0f, 0f, 0f, 0.30f), null, 3010, 0.03f);

        // cover, a little bigger than the open pages
        AddFlat("Cover", spineX, 0f, PageWidth * 2f + 0.11f, PageHeight + 0.11f, CoverColor, null, 3020, 0.025f);

        // stacks of page edges on both outer sides (the "other pages" behind the open ones)
        Color edgeA = new Color(0.90f, 0.87f, 0.78f);
        Color edgeB = new Color(0.80f, 0.77f, 0.68f);
        for (int k = 4; k >= 1; k--)
        {
            Color c = k % 2 == 0 ? edgeA : edgeB;
            AddFlat("EdgeR" + k, k * 0.007f, -k * 0.005f, PageWidth, PageHeight, c, null, 3021 + (4 - k), 0.02f);
            AddFlat("EdgeL" + k, leftX - k * 0.007f, -k * 0.005f, PageWidth, PageHeight, c, null, 3021 + (4 - k), 0.02f);
        }

        // the two open pages
        AddFlat("RightPage", 0f, 0f, PageWidth, PageHeight, Color.white, rightTexture, 3030, 0.01f);
        AddFlat("LeftPage", leftX, 0f, PageWidth, PageHeight, Color.white, leftTexture, 3030, 0.01f);

        // shadow in the fold of the spine
        AddFlat("Spine", spineX, 0f, 0.16f, PageHeight, Color.white, MakeSpineTexture(), 3040, 0.008f);

        // notes on the left page, in pencil, slightly crooked like handwriting
        notes = AddLabel(root, "Notes\n\nRoom A - colours\nRoom B - positions\nRoom C - things that vanish\n\nWhat did you notice?",
                                    new Vector3(leftX * side, 0.05f, 0f), 0.44f, PencilInk,
                                    TextAlignmentOptions.TopLeft, new Vector2(PageWidth - 0.2f, PageHeight - 0.2f),
                                    FontStyles.Normal).gameObject;
        notes.transform.localRotation = Quaternion.Euler(0f, 0f, -2f);
    }

    // a flat coloured (and optionally textured) rectangle behind the text
    private void AddFlat(string name, float viewerRightX, float y, float width, float height,
                         Color color, Texture2D texture, int renderQueue, float distanceBehind)
    {
        GameObject quad = WorldTextBox.CreateBackground(root, flipFacing, distanceBehind);
        quad.name = name;
        quad.transform.localPosition = new Vector3(viewerRightX * side, y, distanceBehind * behind);
        WorldTextBox.Fit(quad, new Vector2(width, height));

        Renderer r = quad.GetComponent<Renderer>();
        Material m = r.material;
        m.color = color;
        if (texture != null) m.mainTexture = texture;
        m.renderQueue = renderQueue;
    }

    // ---- procedural textures --------------------------------------------------------------

    // cream paper with a little grain; the left page also gets ruled lines and a red margin
    private static Texture2D MakePaperTexture(bool ruled)
    {
        const int w = 256, h = 512;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        System.Random rng = new System.Random(ruled ? 7 : 3);
        Color paper = new Color(0.96f, 0.94f, 0.87f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float grain = ((float)rng.NextDouble() - 0.5f) * 0.035f;
                Color c = new Color(paper.r + grain, paper.g + grain, paper.b + grain, 1f);
                if (ruled)
                {
                    if (y % 24 == 0 && y > 40) c = Color.Lerp(c, new Color(0.55f, 0.68f, 0.85f), 0.65f);
                    if (x >= 30 && x <= 31) c = Color.Lerp(c, new Color(0.85f, 0.45f, 0.45f), 0.55f);
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    // dark in the middle, fading to nothing at both sides
    private static Texture2D MakeSpineTexture()
    {
        const int w = 64;
        Texture2D t = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < w; x++)
        {
            float d = Mathf.Abs((x + 0.5f) / w - 0.5f) * 2f; // 0 at the centre, 1 at the sides
            t.SetPixel(x, 0, new Color(0.10f, 0.07f, 0.04f, 0.40f * (1f - d) * (1f - d)));
        }
        t.Apply();
        return t;
    }

    // ---- running the questionnaire --------------------------------------------------------

    /// <summary>Shows every question in turn. onAnswer(question, chosenIndex) is called after each one.</summary>
    public IEnumerator Run(List<QuizQuestion> questions, Action<QuizQuestion, int> onAnswer)
    {
        Place();
        root.gameObject.SetActive(true);
        yield return null; // so the press that started the quiz cannot also pick an answer

        for (int q = 0; q < questions.Count; q++)
        {
            choice = -1;
            if (q > 0) SoundPlayer.Play("page_rustle", root.position, 0.6f); // a quick, light rustle between questions
            BuildPage(questions[q], q, questions.Count);

            yield return new WaitUntil(() => choice >= 0);
            int picked = choice;
            yield return new WaitForSeconds(0.4f); // lets the participant see the cross
            onAnswer(questions[q], picked);
        }
        // the book stays open: the results page comes next
    }

    // ---- results page and the last page ---------------------------------------------------

    /// <summary>Replaces the current page by the results; returns when the participant points at "Turn the page".</summary>
    public IEnumerator ShowResults(ResultsSummary r)
    {
        choice = -1;
        NewPage();

        float contentWidth = PageWidth - 0.14f;
        AddLabel(page, "Your results", new Vector3(0f, 0.365f, 0f), 0.62f,
                 InkColor, TextAlignmentOptions.Left, new Vector2(contentWidth, 0.1f), FontStyles.Normal);

        if (!r.hasData)
        {
            AddLabel(page, "No trials were recorded.", new Vector3(0f, 0.15f, 0f), 0.5f,
                     GreyInk, TextAlignmentOptions.Left, new Vector2(contentWidth, 0.1f), FontStyles.Normal);
        }
        else
        {
            float y = 0.27f;
            const float step = 0.07f;

            AddStatRow("Correct answers", r.correct + " / " + r.total, r.Accuracy, y); y -= step;
            foreach (ResultsSummary.RoomResult room in r.rooms)
            {
                string value = room.total > 0 ? room.correct + " / " + room.total : "-";
                float f = room.total > 0 ? (float)room.correct / room.total : 0f;
                AddStatRow("   " + room.label, value, f, y); y -= step;
            }
            AddStatRow("Average confidence", Format(r.averageConfidence) + " / 5", r.averageConfidence / 5f, y); y -= step;
            AddStatRow("Confidence when right", r.confidenceWhenRight >= 0f ? Format(r.confidenceWhenRight) + " / 5" : "-",
                       Mathf.Max(0f, r.confidenceWhenRight) / 5f, y); y -= step;
            AddStatRow("Confidence when wrong", r.confidenceWhenWrong >= 0f ? Format(r.confidenceWhenWrong) + " / 5" : "-",
                       Mathf.Max(0f, r.confidenceWhenWrong) / 5f, y); y -= step;
            AddStatRow("Average reaction time", Format(r.averageReactionSeconds) + " s", -1f, y);
        }

        BuildButton("Turn the page  >", -0.36f, 0.4f);

        yield return new WaitUntil(() => choice >= 0);
        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>The page turns (it folds towards the spine and opens on the left); the last page says "The End".</summary>
    public IEnumerator TurnToEnd()
    {
        SoundPlayer.Play("page_flip", root.position);

        // the sheet that turns: it lies on the right page and is hinged at the spine
        Transform flipper = new GameObject("Flipper").transform;
        flipper.SetParent(root, false);
        flipper.localPosition = new Vector3(-PageWidth / 2f * side, 0f, 0f);

        GameObject sheet = MakeQuad(flipper, new Vector3(PageWidth / 2f * side, 0f, 0.012f * behind),
                                    new Vector2(PageWidth, PageHeight), Color.white, 3035);
        Material sheetMaterial = sheet.GetComponent<Renderer>().sharedMaterial;
        sheetMaterial.mainTexture = rightTexture;

        page.SetParent(flipper, true); // the results turn with the sheet

        // first half: the sheet folds up against the spine
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            flipper.localScale = new Vector3(Mathf.Max(1f - Smooth(t), 0.001f), 1f, 1f);
            yield return null;
        }
        flipper.localScale = new Vector3(0.001f, 1f, 1f);

        // at the fold: the old page is gone, the last page is revealed, the sheet shows its back
        Destroy(page.gameObject);
        if (notes != null) notes.SetActive(false);
        sheetMaterial.mainTexture = leftTexture;
        sheetMaterial.mainTextureScale = new Vector2(-1f, 1f); // mirrored, because the sheet is about to be turned over
        sheetMaterial.mainTextureOffset = new Vector2(1f, 0f);
        BuildEndPage();

        // second half: it opens onto the left page
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            flipper.localScale = new Vector3(-Mathf.Max(Smooth(t), 0.001f), 1f, 1f);
            yield return null;
        }
        flipper.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void BuildEndPage()
    {
        NewPage();
        float contentWidth = PageWidth - 0.1f;
        AddLabel(page, "The End", new Vector3(0f, 0.14f, 0f), 2.0f,
                 InkColor, TextAlignmentOptions.Center, new Vector2(contentWidth, 0.35f), FontStyles.Normal);
        AddLabel(page, "Thank you for taking part!", new Vector3(0f, -0.14f, 0f), 0.72f,
                 InkColor, TextAlignmentOptions.Center, new Vector2(contentWidth, 0.2f), FontStyles.Normal);
        AddLabel(page, "You can take off the headset now.", new Vector3(0f, -0.31f, 0f), 0.45f,
                 PenInk, TextAlignmentOptions.Center, new Vector2(contentWidth, 0.1f), FontStyles.Normal);
    }

    // one line of the results: name on the left, a bar, the value on the right
    private void AddStatRow(string label, string value, float fraction, float y)
    {
        AddLabel(page, label, new Vector3(-0.145f * side, y, 0f), 0.4f,
                 InkColor, TextAlignmentOptions.Left, new Vector2(0.42f, 0.07f), FontStyles.Normal);
        AddLabel(page, value, new Vector3(0.31f * side, y, 0f), 0.4f,
                 PenInk, TextAlignmentOptions.Right, new Vector2(0.1f, 0.07f), FontStyles.Normal);

        if (fraction < 0f) return; // no bar for this row

        const float barWidth = 0.18f, barHeight = 0.022f, barCenter = 0.165f;
        MakeQuad(page, new Vector3(barCenter * side, y, 0.004f * behind), new Vector2(barWidth, barHeight),
                 new Color(0.78f, 0.75f, 0.67f), 3045);

        float f = Mathf.Clamp01(fraction);
        if (f <= 0f) return;
        float left = barCenter - barWidth / 2f;
        MakeQuad(page, new Vector3((left + barWidth * f / 2f) * side, y, 0.0045f * behind),
                 new Vector2(barWidth * f, barHeight), PenInk, 3046);
    }

    // a pointable line of text (like an answer) that sets "choice" to 0 when selected
    private void BuildButton(string text, float y, float width)
    {
        GameObject row = new GameObject("Button");
        row.transform.SetParent(page, false);
        row.transform.localPosition = new Vector3((PageWidth / 2f - 0.07f - width / 2f) * side, y, 0f);

        BoxCollider collider = row.AddComponent<BoxCollider>();
        collider.size = new Vector3(width, RowHeight, 0.05f);

        GameObject highlight = MakeQuad(row.transform, new Vector3(0f, 0f, 0.005f * behind), new Vector2(width, RowHeight), HighlightColor, 3050);
        highlight.SetActive(false);

        AddLabel(row.transform, text, Vector3.zero, 0.5f, PenInk, TextAlignmentOptions.Right,
                 new Vector2(width - 0.04f, RowHeight), FontStyles.Normal);

        PointerTarget target = row.AddComponent<PointerTarget>();
        target.onHoverEnter.AddListener(() => highlight.SetActive(true));
        target.onHoverExit.AddListener(() => highlight.SetActive(false));
        target.onSelected.AddListener(() => { if (choice < 0) choice = 0; });
    }

    private void NewPage()
    {
        if (page != null) Destroy(page.gameObject);
        page = new GameObject("Page").transform;
        page.SetParent(root, false);
    }

    private GameObject MakeQuad(Transform parent, Vector3 localPosition, Vector2 size, Color color, int renderQueue)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        Material m = new Material(Shader.Find("Sprites/Default")) { color = color };
        m.renderQueue = renderQueue;
        Renderer r = quad.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sharedMaterial = m;
        return quad;
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static string Format(float value)
    {
        return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void BuildPage(QuizQuestion question, int index, int total)
    {
        if (page != null) Destroy(page.gameObject);
        page = new GameObject("Page").transform;
        page.SetParent(root, false);

        float contentWidth = PageWidth - 0.14f;
        AddLabel(page, "Question " + (index + 1) + " of " + total, new Vector3(0f, 0.375f, 0f), 0.34f,
                 GreyInk, TextAlignmentOptions.Left, new Vector2(contentWidth, 0.07f), FontStyles.Normal);
        AddLabel(page, question.text, new Vector3(0f, 0.255f, 0f), 0.5f,
                 InkColor, TextAlignmentOptions.Left, new Vector2(contentWidth, 0.17f), FontStyles.Normal);

        for (int i = 0; i < question.options.Length; i++)
            BuildOption(question.options[i], i, contentWidth + 0.04f);
    }

    private void BuildOption(string optionText, int index, float width)
    {
        GameObject row = new GameObject("Option_" + index);
        row.transform.SetParent(page, false);
        row.transform.localPosition = new Vector3(0f, 0.07f - index * RowStep, 0f);

        BoxCollider collider = row.AddComponent<BoxCollider>();
        collider.size = new Vector3(width, RowHeight, 0.05f);

        // yellow highlighter, above the paper and below the text
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(highlight.GetComponent<Collider>());
        highlight.transform.SetParent(row.transform, false);
        highlight.transform.localPosition = new Vector3(0f, 0f, 0.005f * behind);
        highlight.transform.localScale = new Vector3(width, RowHeight, 1f);
        Renderer r = highlight.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.material = new Material(Shader.Find("Sprites/Default")) { color = HighlightColor };
        r.material.renderQueue = 3050;
        highlight.SetActive(false);

        TextMeshPro label = AddLabel(row.transform, "[   ]  " + optionText, Vector3.zero, 0.42f,
                                     InkColor, TextAlignmentOptions.Left, new Vector2(width - 0.06f, RowHeight), FontStyles.Normal);

        PointerTarget target = row.AddComponent<PointerTarget>();
        target.onHoverEnter.AddListener(() => highlight.SetActive(true));
        target.onHoverExit.AddListener(() => { if (choice < 0) highlight.SetActive(false); });
        target.onSelected.AddListener(() =>
        {
            if (choice >= 0) return; // one answer per question
            label.text = "[ X ]  " + optionText;
            label.color = PenInk;
            highlight.SetActive(true);
            choice = index;
        });
    }

    private TextMeshPro AddLabel(Transform parent, string message, Vector3 localPosition, float size,
                                 Color color, TextAlignmentOptions alignment, Vector2 area, FontStyles style)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        TextMeshPro label = go.AddComponent<TextMeshPro>();
        if (font != null) label.font = font;
        label.text = message;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        label.rectTransform.sizeDelta = area;
        label.fontMaterial.renderQueue = 3100; // after the book, so the paper never covers the text
        return label;
    }

    // the right page 0.9 m in front of where the participant is looking, a little below eye level, facing them
    private void Place()
    {
        Vector3 forward = viewer.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 position = viewer.position + forward * 0.9f;
        position.y = viewer.position.y - 0.1f;
        root.position = position;

        Vector3 toViewer = viewer.position - position;
        toViewer.y = 0f;
        root.rotation = Quaternion.LookRotation(flipFacing ? -toViewer : toViewer);
    }
}
