using UnityEngine;
using UnityEngine.UIElements;

namespace ShooterDem
{
// Reticle con indicadores en ARCO + mira central, dibujado con Painter2D (sin sprites).
// - 4 arcos: vida + escudo (izq), cargador + reserva (der). Color duotono; rojo si bajo.
// - Mira CENTRAL distinta por arma (cruz / circulo / punto) -> estilo Half-Life.
// - Al RECARGAR, los arcos se ocultan y aparece un circulo girando (spinner).
// - Animaciones: lerp de valores + "kick" (abrir/cerrar) al disparar.
// Vida y cargador llevan datos reales; escudo y reserva van a 1 (placeholder).
public class CrosshairArcs : VisualElement
{
    private float tHealth = 1f, tShield = 1f, tMag = 1f, tReserve = 1f;  // objetivo
    private float dHealth = 1f, dShield = 1f, dMag = 1f, dReserve = 1f;  // mostrado (animado)
    public float Health   { set { tHealth = Mathf.Clamp01(value); } }
    public float Shield   { set { tShield = Mathf.Clamp01(value); } }
    public float Magazine { set { tMag = Mathf.Clamp01(value); } }
    public float Reserve  { set { tReserve = Mathf.Clamp01(value); } }

    private bool reloading;
    public bool Reloading { set { if (reloading != value) { reloading = value; MarkDirtyRepaint(); } } }

    private CrosshairStyle reticle = CrosshairStyle.Cross;
    public CrosshairStyle Reticle { set { reticle = value; MarkDirtyRepaint(); } }

    // Radio del circulo (estilo escopeta): refleja la dispersion del arma.
    private float circleRadius = 9f;
    public float CircleRadius { set { circleRadius = Mathf.Max(2f, value); MarkDirtyRepaint(); } }

    // Lo llama el HUD en cada disparo: los arcos se "abren" y vuelven solos.
    public void Kick() { kick = 1f; MarkDirtyRepaint(); }

    const float InnerRadius = 28f;
    const float OuterRadius = 40f;
    const float Thickness = 5f;
    const float HalfSweep = 42f;
    const float AnimSpeed = 8f;
    const float LowThreshold = 0.3f;
    const float KickExpand = 9f;     // px que se abren los arcos al disparar
    const float SpinSpeed = 7f;      // grados por tick del spinner de recarga

    // Si la descarga sale al reves, cambia esto a false.
    const bool DrainFromTop = true;

    private float kick;
    private float spin;

    // Indicadores direccionales de dano (estilo CS): arcos rojos alrededor de la mira
    // que apuntan al origen del golpe y se desvanecen.
    const int MaxDamage = 6;
    const float DamageLife = 1.3f;                      // segundos que dura cada indicador
    readonly float[] dmgAngle = new float[MaxDamage];   // angulo: 0=de frente, +=derecha
    readonly float[] dmgLife = new float[MaxDamage];    // 1..0 (se desvanece)

    // Lo llama el HUD al recibir dano. angleFromFront: 0=de frente, +90=derecha,
    // -90=izquierda, +-180=a la espalda.
    public void AddDamage(float angleFromFront)
    {
        int slot = 0; float lowest = float.MaxValue;        // reusa el slot mas gastado
        for (int i = 0; i < MaxDamage; i++)
            if (dmgLife[i] < lowest) { lowest = dmgLife[i]; slot = i; }
        dmgAngle[slot] = angleFromFront;
        dmgLife[slot] = 1f;
        MarkDirtyRepaint();
    }

    static readonly Color Track = new Color(0.843f, 0.910f, 0.816f, 0.15f);
    static readonly Color Bone  = new Color(0.843f, 0.910f, 0.816f);
    static readonly Color Warn  = new Color(0.90f, 0.20f, 0.15f);

    public CrosshairArcs()
    {
        style.position = Position.Absolute;
        style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnGenerate;
        schedule.Execute(Tick).Every(16);
    }

    void Tick()
    {
        float k = Mathf.Clamp01(AnimSpeed * 0.016f);
        bool changed = false;
        Step(ref dHealth, tHealth, k, ref changed);
        Step(ref dShield, tShield, k, ref changed);
        Step(ref dMag, tMag, k, ref changed);
        Step(ref dReserve, tReserve, k, ref changed);

        if (kick > 0.001f)
        {
            kick = Mathf.Lerp(kick, 0f, 0.2f);
            if (kick < 0.001f) kick = 0f;
            changed = true;
        }
        if (reloading)
        {
            spin = (spin + SpinSpeed) % 360f;   // gira mientras recarga
            changed = true;
        }

        for (int i = 0; i < MaxDamage; i++)
            if (dmgLife[i] > 0f)
            {
                dmgLife[i] = Mathf.Max(0f, dmgLife[i] - 0.016f / DamageLife);
                changed = true;
            }

        if (changed) MarkDirtyRepaint();
    }

    static void Step(ref float cur, float target, float k, ref bool changed)
    {
        if (Mathf.Abs(cur - target) < 0.001f)
        {
            if (cur != target) { cur = target; changed = true; }
            return;
        }
        cur = Mathf.Lerp(cur, target, k);
        changed = true;
    }

    void OnGenerate(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        Vector2 c = contentRect.center;
        p.lineCap = LineCap.Round;

        if (reloading)
        {
            DrawSpinner(p, c);   // recargando: circulo girando en lugar de los arcos
        }
        else
        {
            DrawStat(p, c, InnerRadius, 180f, dHealth);  // vida
            DrawStat(p, c, OuterRadius, 180f, dShield);  // escudo
            DrawStat(p, c, InnerRadius, 0f, dMag);       // cargador
            DrawStat(p, c, OuterRadius, 0f, dReserve);   // reserva
        }

        DrawReticle(p, c);   // mira central segun el arma (siempre visible)
        DrawDamage(p, c);    // indicadores direccionales de dano (si los hay)
    }

    // Arcos rojos que apuntan al origen del dano reciente y se desvanecen.
    void DrawDamage(Painter2D p, Vector2 c)
    {
        const float r = 54f, half = 22f;
        p.lineWidth = 6f;
        for (int i = 0; i < MaxDamage; i++)
        {
            if (dmgLife[i] <= 0f) continue;
            // En pantalla Painter2D 0deg=derecha y crece en horario; "de frente"=arriba=270.
            float screen = 270f + dmgAngle[i];
            Color col = Warn; col.a = Mathf.Clamp01(dmgLife[i]);
            p.strokeColor = col;
            p.BeginPath();
            p.Arc(c, r, Deg(screen - half), Deg(screen + half));
            p.Stroke();
        }
    }

    // Circulo casi-completo que gira: indica "recargando, no puedes disparar".
    void DrawSpinner(Painter2D p, Vector2 c)
    {
        p.strokeColor = Bone;
        p.lineWidth = 4f;
        p.BeginPath();
        p.Arc(c, 16f, Deg(spin), Deg(spin + 280f));   // circulo de recarga, mas pequeno
        p.Stroke();
    }

    // Mira central: cambia de forma segun el arma equipada.
    void DrawReticle(Painter2D p, Vector2 c)
    {
        p.strokeColor = Bone;
        p.fillColor = Bone;

        switch (reticle)
        {
            case CrosshairStyle.Dot:
                p.BeginPath();
                p.Arc(c, 2.5f, Deg(0), Deg(360));
                p.Fill();
                break;

            case CrosshairStyle.Circle:
                p.lineWidth = 2f;
                p.BeginPath();
                p.Arc(c, circleRadius, Deg(0), Deg(360));
                p.Stroke();
                break;

            default: // Cross: cuatro marcas con hueco central
                p.lineWidth = 2f;
                float g = 4f, len = 7f;
                Line(p, new Vector2(c.x, c.y - g), new Vector2(c.x, c.y - g - len));
                Line(p, new Vector2(c.x, c.y + g), new Vector2(c.x, c.y + g + len));
                Line(p, new Vector2(c.x - g, c.y), new Vector2(c.x - g - len, c.y));
                Line(p, new Vector2(c.x + g, c.y), new Vector2(c.x + g + len, c.y));
                break;
        }
    }

    void Line(Painter2D p, Vector2 a, Vector2 b)
    {
        p.BeginPath();
        p.MoveTo(a);
        p.LineTo(b);
        p.Stroke();
    }

    void DrawStat(Painter2D p, Vector2 c, float radius, float centerAngle, float pct)
    {
        radius += kick * KickExpand;   // al disparar, el arco se abre hacia afuera
        float a = centerAngle - HalfSweep;
        float b = centerAngle + HalfSweep;
        float anchor = DrainFromTop ? a : b;
        float tip = DrainFromTop ? b : a;

        p.strokeColor = Track;
        p.lineWidth = Thickness;
        p.BeginPath();
        p.Arc(c, radius, Deg(Mathf.Min(a, b)), Deg(Mathf.Max(a, b)));
        p.Stroke();

        if (pct > 0f)
        {
            float fillEnd = Mathf.Lerp(anchor, tip, pct);
            p.strokeColor = pct <= LowThreshold ? Warn : Bone;
            p.BeginPath();
            p.Arc(c, radius, Deg(Mathf.Min(anchor, fillEnd)), Deg(Mathf.Max(anchor, fillEnd)));
            p.Stroke();
        }
    }

    static Angle Deg(float d) => new Angle(d, AngleUnit.Degree);
}
}
