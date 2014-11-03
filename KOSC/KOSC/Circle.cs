using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace KOSC
{
    /// <summary>
    /// A class that represents a joint on the selection of what OSC messages to output
    /// </summary>
    public class Circle
    {
        /// <summary>
        ///The x-co ordinate of the circle
        /// </summary>
        private float xPos;
        /// <summary>
        ///The y-co ordinate of the circle
        /// </summary>
        private float yPos;
        /// <summary>
        ///The radius of the circle
        /// </summary>
        private float radius;
        /// <summary>
        ///the point positions of the circle that is created, represents all 360 points
        /// </summary>
        private VertexPositionColor[] points;
        private BasicEffect effect;
        /// <summary>
        ///states whether the mouse is over the circle or not
        /// </summary>
        private Boolean mouseOverCircle;
        /// <summary>
        ///states whether the mouse has been over the circle and if the circle is active (mouse has been pressed on it)
        /// </summary>
        private Boolean active;
        /// <summary>
        ///the name of the joint the circle represents
        /// </summary>
        private String jointName;

        public Circle(float _x, float _y, int rad, String _jointName)
        {
            xPos = _x;
            yPos = _y;
            radius = rad;
            points = new VertexPositionColor[360];
            jointName = _jointName;
            mouseOverCircle = false;
            active = false;
            setupCircle();
        }

        /// <summary>
        ///sets up the viewing matrix so that we can see the circles in our viewport
        /// </summary>
        public void setupCircleEffect(GraphicsDeviceManager graphics)
        {
            effect = new BasicEffect(graphics.GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.Projection = Matrix.CreateOrthographicOffCenter
               (0, graphics.GraphicsDevice.Viewport.Width,     // left, right
                graphics.GraphicsDevice.Viewport.Height, 0,    // bottom, top
                0, 1);                                         // near, far plane

        }

        /// <summary>
        ///sets up the circles points based on the x,y and radius parameters
        /// </summary>
        private void setupCircle()
        {
            float thetaInc = (((float)Math.PI * 2) / points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                double theta = (thetaInc * i);
                double xInc = Math.Sin(theta);
                double yInc = Math.Cos(theta);
                points[i].Position = new Vector3((xPos + (float)(xInc * radius)), (yPos + (float)(yInc * radius)), 0f);
                points[i].Color = Color.Red;
            }
        }

        /// <summary>
        ///will draw the circle
        /// </summary>
        public void drawCircle(GraphicsDeviceManager graphics)
        {    
            effect.CurrentTechnique.Passes[0].Apply();
            graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, points, 0, points.Length - 1);
        }

        /// <summary>
        ///checks whether the mouse is over the circle or not, if it is then circle colour changes to Cyan
        /// </summary>
        public void mouseOver(int mouseX, int mouseY)
        {
            float distX = (xPos - mouseX);
            float distY = (yPos - mouseY);
            if (Math.Sqrt((distX * distX) + (distY * distY)) < radius)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].Color = Color.Cyan;
                }
                mouseOverCircle = true;
            }
            else
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].Color = Color.Red;
                }
                mouseOverCircle = false;
            }        
        }

        /// <summary>
        ///getter for returning over circle status
        /// </summary>
        public Boolean getOverCircle()
        {
            return mouseOverCircle;
        }

        /// <summary>
        ///getter for returning the joint name the circle represents
        /// </summary>
        public String getJointName()
        {
            return jointName;
        }

        /// <summary>
        ///getter for returning the active status of the circle
        /// </summary>
        public Boolean GetActiveStatus()
        {
            return active;
        }

        /// <summary>
        ///setter to set the active status of the circle
        /// </summary>
        public void SetActiveStatus(Boolean condition)
        {
            active = condition;
        }

        /// <summary>
        ///based on if the circle is active, will change the colour to green.
        /// </summary>
        public void ActiveAction()
        {
            if (active)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].Color = Color.Green;
                }
            }
        }
    }
}
