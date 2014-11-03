using System;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Bespoke.Common.Osc;
using System.Threading;
using System.Net;
using System.Collections.Generic;

namespace KOSC
{
    /// <summary>
    /// A delegate method explaining how to map a SkeletonPoint from one space to another.
    /// </summary>
    /// <param name="point">The SkeletonPoint to map.</param>
    /// <returns>The Vector2 representing the target location.</returns>
    public delegate Vector2 SkeletonPointMap(SkeletonPoint point);

    public class SkeletonStreamRenderer : Object2D
    {
        /// <summary>
        /// This is the map method called when mapping from
        /// skeleton space to the target space.
        /// </summary>
        private readonly SkeletonPointMap mapMethod;

        /// <summary>
        /// The last frames skeleton data.
        /// </summary>
        private static Skeleton[] skeletonData;

        /// <summary>
        /// This flag ensures only request a frame once per update call
        /// across the entire application.
        /// </summary>
        private static bool skeletonDrawn = true;

        /// <summary>
        /// The origin (center) location of the joint texture.
        /// </summary>
        private Vector2 jointOrigin;

        /// <summary>
        /// The joint texture.
        /// </summary>
        private Texture2D jointTexture;

        /// <summary>
        /// The origin (center) location of the bone texture.
        /// </summary>
        private Vector2 boneOrigin;

        /// <summary>
        /// The bone texture.
        /// </summary>
        private Texture2D boneTexture;

        /// <summary>
        /// Whether the rendering has been initialized.
        /// </summary>
        private bool initialized;


        /// <summary>
        /// determines what skeleton to choose based on defined heuristics
        /// </summary>
        private KinectSkeletonChooser skelChooser;

        private List<String> selectedJointsToOutput = new List<String>();

        /// <summary>
        /// Initializes a new instance of the SkeletonStreamRenderer class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        /// <param name="map">The method used to map the SkeletonPoint to the target space.</param>
        public SkeletonStreamRenderer(Game game, SkeletonPointMap map)
            : base(game)
        {
            this.mapMethod = map;
            skelChooser = new KinectSkeletonChooser(this);
        }

        public KinectSkeletonChooser SkelChooser
        {
            get { return this.skelChooser; }
        }

        /// <summary>
        /// This method initializes necessary values.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            this.Size = new Vector2(400, 300);
            this.Position = new Vector2(10, 40);
            this.initialized = true;
            
        }

        /// <summary>
        /// This method retrieves a new skeleton frame if necessary.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // If the sensor is not found, not running, or not connected, stop now
            if (null == this.Chooser.Sensor ||
                false == this.Chooser.Sensor.IsRunning ||
                KinectStatus.Connected != this.Chooser.Sensor.Status)
            {
                return;
            }

            // If we have already drawn this skeleton, then we should retrieve a new frame
            // This prevents us from calling the next frame more than once per update
            if (skeletonDrawn)
            {
                using (var skeletonFrame = this.Chooser.Sensor.SkeletonStream.OpenNextFrame(0))
                {
                    // Sometimes we get a null frame back if no data is ready
                    if (null == skeletonFrame)
                    {
                        return;
                    }

                    // Reallocate if necessary
                    if (null == skeletonData || skeletonData.Length != skeletonFrame.SkeletonArrayLength)
                    {
                        skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                    skelChooser.ChooseTrackedSkeletons(skeletonData);
                    skeletonDrawn = false;
                }
            }
        }

        /// <summary>
        /// This method draws the skeleton frame data.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public override void Draw(GameTime gameTime)
        {
            // If the joint texture isn't loaded, load it now
            if (null == this.jointTexture)
            {
                this.LoadContent();
            }

            // If we don't have data, lets leave
            if (null == skeletonData || null == this.mapMethod)
            {
                return;
            }

            if (false == this.initialized)
            {
                this.Initialize();
            }

            this.SharedSpriteBatch.Begin();

            foreach (var skeleton in skeletonData)
            {
                switch (skeleton.TrackingState)
                {
                    case SkeletonTrackingState.Tracked:
                        // Draw Bones
                        this.DrawBone(skeleton.Joints, JointType.Head, JointType.ShoulderCenter);
                        this.DrawBone(skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderLeft);
                        this.DrawBone(skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight);
                        this.DrawBone(skeleton.Joints, JointType.ShoulderCenter, JointType.Spine);
                        this.DrawBone(skeleton.Joints, JointType.Spine, JointType.HipCenter);
                        this.DrawBone(skeleton.Joints, JointType.HipCenter, JointType.HipLeft);
                        this.DrawBone(skeleton.Joints, JointType.HipCenter, JointType.HipRight);

                        this.DrawBone(skeleton.Joints, JointType.ShoulderLeft, JointType.ElbowLeft);
                        this.DrawBone(skeleton.Joints, JointType.ElbowLeft, JointType.WristLeft);
                        this.DrawBone(skeleton.Joints, JointType.WristLeft, JointType.HandLeft);

                        this.DrawBone(skeleton.Joints, JointType.ShoulderRight, JointType.ElbowRight);
                        this.DrawBone(skeleton.Joints, JointType.ElbowRight, JointType.WristRight);
                        this.DrawBone(skeleton.Joints, JointType.WristRight, JointType.HandRight);

                        this.DrawBone(skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                        this.DrawBone(skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                        this.DrawBone(skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);

                        this.DrawBone(skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                        this.DrawBone(skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                        this.DrawBone(skeleton.Joints, JointType.AnkleRight, JointType.FootRight);

                        // Now draw the joints
                        foreach (Joint j in skeleton.Joints)
                        {
                            Color jointColor = Color.Green;
                            if (j.TrackingState != JointTrackingState.Tracked)
                            {
                                jointColor = Color.Yellow;
                            }

                            this.SharedSpriteBatch.Draw(
                                this.jointTexture,
                                this.mapMethod(j.Position),
                                null,
                                jointColor,
                                0.0f,
                                this.jointOrigin,
                                1.0f,
                                SpriteEffects.None,
                                0.0f);
                        }
                        sendOSCMessages(skeleton);
                        break;
                    case SkeletonTrackingState.PositionOnly:
                        // If we are only tracking position, draw a blue dot
                        this.SharedSpriteBatch.Draw(
                                this.jointTexture,
                                this.mapMethod(skeleton.Position),
                                null,
                                Color.Blue,
                                0.0f,
                                this.jointOrigin,
                                1.0f,
                                SpriteEffects.None,
                                0.0f);
                        break;
                }
            }

            this.SharedSpriteBatch.End();
            skeletonDrawn = true;

            base.Draw(gameTime);
        }

        /// <summary>
        /// This method loads the textures and sets the origin values.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            this.jointTexture = Game.Content.Load<Texture2D>("Joint");
            this.jointOrigin = new Vector2(this.jointTexture.Width / 2, this.jointTexture.Height / 2);

            this.boneTexture = Game.Content.Load<Texture2D>("Bone");
            this.boneOrigin = new Vector2(0.5f, 0.0f);
        }

        /// <summary>
        /// This method draws a bone.
        /// </summary>
        /// <param name="joints">The joint data.</param>
        /// <param name="startJoint">The starting joint.</param>
        /// <param name="endJoint">The ending joint.</param>
        private void DrawBone(JointCollection joints, JointType startJoint, JointType endJoint)
        {
            Vector2 start = this.mapMethod(joints[startJoint].Position);
            Vector2 end = this.mapMethod(joints[endJoint].Position);
            Vector2 diff = end - start;
            Vector2 scale = new Vector2(1.0f, diff.Length() / this.boneTexture.Height);

            float angle = (float)Math.Atan2(diff.Y, diff.X) - MathHelper.PiOver2;

            Color color = Color.LightGreen;
            if (joints[startJoint].TrackingState != JointTrackingState.Tracked ||
                joints[endJoint].TrackingState != JointTrackingState.Tracked)
            {
                color = Color.Gray;
            }

            this.SharedSpriteBatch.Draw(this.boneTexture, start, null, color, angle, this.boneOrigin, scale, SpriteEffects.None, 1.0f);
        }

        private void sendOSCMessages(Skeleton skel)
        {
            /*we need to check through all the buttons to see what joints we're going to output over OSC
             * to do this we should check through to see which ones are active, get the joint name for a 
             * string comparison and then based on whether a string matches we should send out the active joint
             */
            
            for (int i = 0; i < this.SharedCircleData.Length; i++)
            {
                if (this.SharedCircleData[i].GetActiveStatus())
                {
                    this.selectedJointsToOutput.Add(this.SharedCircleData[i].getJointName());
                }
            }

            IPEndPoint sourceEndPoint = new IPEndPoint(this.SharedOSCData.getOscIpAddress(), this.SharedOSCData.getPortNumber());
            OscMessage message = new OscMessage(sourceEndPoint, this.SharedOSCData.getStartString() + this.SharedOSCData.getStartChannel());//needs to be customisable to any start channel
            float offXFactor = 0.15f;

            for (int i = 0; i < this.selectedJointsToOutput.Count; i++)
            {
                if (this.selectedJointsToOutput[i].Equals("Head", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.Head].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.Head].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Centre Shoulder", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.ShoulderCenter].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.ShoulderCenter].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Shoulder", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.ShoulderLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.ShoulderLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Shoulder", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.ShoulderRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.ShoulderRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Elbow", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.ElbowLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.ElbowLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Elbow", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.ElbowRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.ElbowRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Wrist", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.WristLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.WristLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Wrist", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.WristRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.WristRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Hand", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.HandLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.HandLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Hand", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.HandRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.HandRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Centre Hip", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.HipCenter].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.HipCenter].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Hip", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.HipLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.HipLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Hip", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.HipRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.HipRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Knee", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.KneeLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.KneeLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Knee", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.KneeRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.KneeRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Ankle", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.AnkleLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.AnkleLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Ankle", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.AnkleRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.AnkleRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Left Foot", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.FootLeft].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.FootLeft].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Right Foot", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.FootRight].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.FootRight].Position.Y) + 0.5f);
                }
                else if (this.selectedJointsToOutput[i].Equals("Spine", StringComparison.OrdinalIgnoreCase))
                {
                    message.Append((skel.Joints[JointType.Spine].Position.X) + offXFactor);
                    message.Append((skel.Joints[JointType.Spine].Position.Y) + 0.5f);
                }
            }
            message.Send(sourceEndPoint);       
            selectedJointsToOutput.Clear();
        }
    }
}