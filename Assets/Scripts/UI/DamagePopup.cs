using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private TextMesh shadowMesh;
    private MeshRenderer meshRenderer;
    private MeshRenderer shadowRenderer;

    private Vector3 moveVelocity;
    private float lifetime = 0.7f;
    private float timer = 0f;
    private float initialScale = 1f;
    private Color textColor = Color.yellow;
    private Color shadowColor = new Color(0f, 0f, 0f, 0.85f);

    public void Setup(int damage, bool isCrit)
    {
        // 1. Khởi tạo TextMesh chính
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
            textMesh = gameObject.AddComponent<TextMesh>();

        meshRenderer = GetComponent<MeshRenderer>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (defaultFont != null)
        {
            textMesh.font = defaultFont;
            if (meshRenderer != null && defaultFont.material != null)
            {
                meshRenderer.sharedMaterial = defaultFont.material;
            }
        }

        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = 0.08f;
        textMesh.fontSize = isCrit ? 60 : 48;
        textMesh.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Bold;

        // Định dạng màu sắc & kích thước theo độ mạnh của đòn đánh
        if (isCrit || damage >= 20)
        {
            textMesh.text = damage + "!";
            textColor = new Color(1f, 0.22f, 0.22f, 1f); // Đỏ rực đòn kết liễu / bạo kích
            initialScale = 1.35f;
        }
        else if (damage >= 15)
        {
            textMesh.text = damage.ToString();
            textColor = new Color(1f, 0.65f, 0.15f, 1f); // Cam sáng đòn 2
            initialScale = 1.15f;
        }
        else
        {
            textMesh.text = damage.ToString();
            textColor = new Color(1f, 0.95f, 0.45f, 1f); // Vàng sáng đòn 1
            initialScale = 0.95f;
        }

        textMesh.color = textColor;
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 600;

        // 2. Tạo bóng chữ (Drop Shadow) an toàn phía sau
        try
        {
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(transform, false);
            shadowObj.transform.localPosition = new Vector3(0.04f, -0.04f, 0.01f);
            shadowObj.transform.localScale = Vector3.one;

            shadowMesh = shadowObj.AddComponent<TextMesh>();
            shadowRenderer = shadowObj.GetComponent<MeshRenderer>();
            if (defaultFont != null)
            {
                shadowMesh.font = defaultFont;
                if (shadowRenderer != null && defaultFont.material != null)
                {
                    shadowRenderer.sharedMaterial = defaultFont.material;
                }
            }
            shadowMesh.alignment = textMesh.alignment;
            shadowMesh.anchor = textMesh.anchor;
            shadowMesh.characterSize = textMesh.characterSize;
            shadowMesh.fontSize = textMesh.fontSize;
            shadowMesh.fontStyle = textMesh.fontStyle;
            shadowMesh.text = textMesh.text;
            shadowMesh.color = shadowColor;
            if (shadowRenderer != null)
                shadowRenderer.sortingOrder = 599;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DamagePopup] Không tạo được bóng chữ: " + ex.Message);
        }

        // 3. Vận tốc bay hình vòng cung (Arc trajectory)
        float randomX = Random.Range(-0.45f, 0.45f);
        float upwardVelocity = isCrit ? 3.4f : 2.8f;
        moveVelocity = new Vector3(randomX, upwardVelocity, 0f);

        transform.localScale = Vector3.one * (initialScale * 1.3f);
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Di chuyển bay lên và giảm dần vận tốc theo trọng lực nhẹ
        transform.position += moveVelocity * Time.deltaTime;
        moveVelocity.y -= 3.2f * Time.deltaTime;
        moveVelocity.x *= 0.96f;

        // Scale punch: Co lại kích thước chuẩn trong 0.12s đầu
        if (timer < 0.12f)
        {
            float progress = timer / 0.12f;
            transform.localScale = Vector3.Lerp(Vector3.one * (initialScale * 1.3f), Vector3.one * initialScale, progress);
        }
        else
        {
            transform.localScale = Vector3.one * initialScale;
        }

        // Mờ dần (Fade out) trong 0.25s cuối
        float fadeStartTime = lifetime - 0.25f;
        if (timer > fadeStartTime)
        {
            float alpha = Mathf.Clamp01(1f - (timer - fadeStartTime) / 0.25f);

            Color c = textColor;
            c.a = alpha;
            textMesh.color = c;

            Color sc = shadowColor;
            sc.a = alpha * 0.85f;
            if (shadowMesh != null)
                shadowMesh.color = sc;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
