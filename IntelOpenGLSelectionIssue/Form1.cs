using System;
using System.Drawing;
using System.Windows.Forms;
using SharpGL;

namespace IntelOpenGLSelectionIssue
{
    public partial class Form1 : Form
    {
        private int selectedIndex = -1;
        private Point pickPoint;

        public Form1()
        {
            // Constructor - set up windows forms and event handlers
            InitializeComponent();

            this.openGLControl1.OpenGLDraw += OpenGLControl1_OpenGLDraw;
            this.openGLControl1.MouseClick += OpenGLControl1_MouseClick;

            string cardVendor = this.openGLControl1.OpenGL.GetString(OpenGL.GL_VENDOR);
            string cardModel = this.openGLControl1.OpenGL.GetString(OpenGL.GL_RENDERER);

            this.Text = $"Intel Selection Issue ({cardVendor} - {cardModel})";
        }

        private void OpenGLControl1_MouseClick(object sender, MouseEventArgs e)
        {
            // This does the pick of one of the circles
            this.pickPoint = new Point(e.Location.X, this.openGLControl1.Height - e.Location.Y);

            var openGL = this.openGLControl1.OpenGL;

            // Switch into OpenGL Select Mode
            uint[] selectBuffer = new uint[2048];
            openGL.SelectBuffer(selectBuffer.Length, selectBuffer);
            openGL.RenderMode(OpenGL.GL_SELECT);

            // Call the Draw code, telling it we're picking.
            this.Draw(openGL, true);

            // Switch back to Render Mode, which gives the # of "hits" in the select buffer
            int hits = openGL.RenderMode(OpenGL.GL_RENDER);
            this.selectedIndex = -1;

            // If you've clicked close to the line that is drawn for a circle, this should return only 1.
            // On our Intel UHD graphics cards, we're getting 50 hits - this is as many hits as there are circles
            // being drawn on the screen.  So no matter which circle is clicked on, it always selects the *last* circle.
            // .
            // Note our actual app (not this test program) behaves a little differently - it doesn't just select "all" items,
            // but it still incorrectly picks things that are outside the pick matrix (and even outside the current viewport).
            // .
            // Running this test program with the software-based GDI renderer or on the NVidia card in our laptops selects the correct
            // circle every time.
            if (hits != 0)
            {
                // Iterate the hits
                int currentIndex = 0;
                for (int i = 0; i < hits; i++)
                {
                    // Selection buffer first 3 elements are the number of number of names in the hit, and the min and max z values for the items in the hit. 
                    uint numNames = selectBuffer[currentIndex++];
                    uint minZ = selectBuffer[currentIndex++];
                    uint maxZ = selectBuffer[currentIndex++];

                    // The pick name is next
                    uint pickedId = selectBuffer[currentIndex++];

                    // And any other pushed pick names are after that.
                    for (int j = 1; j < numNames; j++)
                    {
                        currentIndex++;
                    }

                    // Set the selected circle index to the picked circle index.
                    this.selectedIndex = (int)pickedId;
                }
            }

            // And force the control to redraw itself.
            this.openGLControl1.Invalidate();
        }

        private void OpenGLControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {
            // This is called by the control to do the draw.
            var openGL = this.openGLControl1.OpenGL;

            this.Draw(openGL, false);
        }

        private void Draw(OpenGL openGL, bool picking)
        {
            openGL.ClearColor(0, 0, 0, 0);
            openGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            // Set up the projection matrix as an Orthographic projection
            openGL.MatrixMode(OpenGL.GL_PROJECTION);
            openGL.LoadIdentity();

            if (picking)
            {
                // When we're picking, setting up a pick matrix restricts the elements that will be selected to
                // a point, defining the center of an on-screen rectangle and a width/height of that rectangle for
                // pick tolerance.
                int[] viewport = new int[4];
                openGL.GetInteger(OpenGL.GL_VIEWPORT, viewport);
                openGL.PickMatrix(this.pickPoint.X, this.pickPoint.Y, 5, 5, viewport);
            }

            float aspect = (float)this.openGLControl1.Height / (float)this.openGLControl1.Size.Width;

            openGL.Ortho(-500, 500, -500 * aspect, 500 * aspect, 1, -1);

            // Back to the modelview matrix, just make this demo easier and leave it as the 'identity' matrix with no transforms.
            openGL.MatrixMode(OpenGL.GL_MODELVIEW);
            openGL.LoadIdentity();

            // Draw a set of circles, highlighting the last picked one if there was one.
            for (int i = 1; i <= 50; i++)
            {
                if (i == selectedIndex)
                {
                    openGL.Color(0f, 1f, 0f);
                }
                else
                {
                    openGL.Color(1f, 0f, 0f);
                }

                this.DrawCircle(openGL, i * 10, picking, i);
            }

            openGL.Flush();
        }

        private void DrawCircle(OpenGL openGL, float radius, bool picking, int pickIndex)
        {
            // 100 points per circle drawn at a fixed radius.
            int numPoints = 100;
            double delta = (Math.PI * 2) / numPoints;

            if (picking)
            {
                // If we're picking, push the pick index and pop it after draw.
                openGL.PushName((uint)pickIndex);
            }

            openGL.Begin(SharpGL.Enumerations.BeginMode.LineLoop);

            for (int i = 0; i <= numPoints; i++)
            {
                double x = Math.Cos(i * delta) * radius;
                double y = Math.Sin(i * delta) * radius;

                openGL.Vertex(x, y);
            }

            openGL.End();

            if (picking)
            {
                openGL.PopName();
            }
        }
    }
}
