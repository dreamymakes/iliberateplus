using System.Drawing;
using System.Windows.Forms;

public class TransparentPictureBox : Control
{
    public Image Image { get; set; }

    public TransparentPictureBox()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Image != null)
        {
            e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            e.Graphics.DrawImage(Image, new Rectangle(0, 0, Width, Height));
        }
    }
}
