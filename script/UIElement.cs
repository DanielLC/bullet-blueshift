using Godot;

public partial class UIElement : Control
{
    private Label label;
    private TextureRect spriteRect;

    private string text;
    //private string spritePath;
    private string backgroundColor;

    private Color _bgColor = Colors.Transparent;

    public UIElement(float x, float y, float width, float height)
    {
        SetNormalizedRect(new Vector2(x, y), new Vector2(width, height));
        Player.uiRoot.AddChild(this);
    }

    private void SetNormalizedRect(Vector2 pos, Vector2 size)
    {
        Vector2 viewVec = Player.Instance.GetViewport().GetVisibleRect().Size;

        Position = (pos + Vector2.One) * 0.5f * viewVec;
        Size = size * 0.5f * viewVec;
    }

    public string Text
    {
        get => text;
        set
        {
            text = value;

            if (value == null)
            {
                if (label != null)
                    label.Visible = false;
                return;
            }

            if (label == null)
            {
                label = new Label();
                AddChild(label);
            }

            label.Visible = true;
            label.Text = value;
        }
    }

    public void SetSprite(string spritePath, int lineNumber)
    {
        if (spritePath == null)
        {
            if (spriteRect != null)
                spriteRect.Visible = false;
            return;
        }

        if (spriteRect == null)
        {
            spriteRect = new TextureRect();
            spriteRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            spriteRect.StretchMode = TextureRect.StretchModeEnum.Scale;
            spriteRect.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(spriteRect);
        }

        spriteRect.Visible = true;
        spriteRect.Texture = Entity.GetTexture(spritePath, lineNumber);
    }

    public string Background
    {
        get => backgroundColor;
        set
        {
            backgroundColor = value;

            if (value == null)
            {
                _bgColor = Colors.Transparent;
            }
            else
            {
                _bgColor = new Color(value);
            }

            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_bgColor.A > 0)
            DrawRect(new Rect2(Vector2.Zero, Size), _bgColor);
    }
}