using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KOSC
{
    /// <summary>
    /// A very basic game component to track common values.
    /// </summary>
    public class Object2D : DrawableGameComponent
    {
        public Object2D(Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Gets or sets the position of the object.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Gets or sets the size of the object.
        /// </summary>
        public Vector2 Size { get; set; }

        /// <summary>
        /// Gets the KinectChooser from the services.
        /// </summary>
        public KinectChooser Chooser
        {
            get
            {
                return (KinectChooser)this.Game.Services.GetService(typeof(KinectChooser));
            }
        }

        /// <summary>
        /// Gets the SpriteBatch from the services.
        /// </summary>
        public SpriteBatch SharedSpriteBatch
        {
            get
            {
                return (SpriteBatch)this.Game.Services.GetService(typeof(SpriteBatch));
            }
        }

        /// <summary>
        /// Gets the Cirle data from the services.
        /// </summary>
        public Circle[] SharedCircleData
        {
            get
            {
                return (Circle[])this.Game.Services.GetService(typeof(Circle[]));
            }
        }

        /// <summary>
        /// Gets the shared OSC Data from the services.
        /// </summary>
        public OSCInfoObject SharedOSCData
        {
            get
            {
                return (OSCInfoObject)this.Game.Services.GetService(typeof(OSCInfoObject));
            }
        }
    }
}
