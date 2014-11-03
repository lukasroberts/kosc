using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;
using Nuclex.UserInterface.Controls.Desktop;
using Nuclex.UserInterface.Visuals.Flat;
using Nuclex.Input;
using Nuclex.UserInterface;

namespace KOSC
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {

        private const int width = 800;
        private const int height = 600;
        private readonly GraphicsDeviceManager graphics;
        private readonly KinectChooser chooser;
        private readonly ColorStreamRenderer colorStream;
        private readonly Rectangle viewPortRectangle;
        private SpriteBatch spriteBatch;
        private SpriteFont font;
        private SpriteFont labelFont;
        private Texture2D humanRigPicture;
        private Circle[] circles;
        private MouseState ms;
        private int index;
        private String currentJoint;
        private List<String> jointList;
        private GuiManager gui;
        private InputManager input;
        private ListControl oscList;
        private InputControl portInput;
        private InputControl ipInput;
        private InputControl startAddressInput;
        private InputControl startMessageInput;
        private ChoiceControl lightJamsOption;
        private ChoiceControl customOption;
        private ChoiceControl[] trackingControls;
        private ButtonControl kinectTiltUp;
        private ButtonControl kinectTiltDown;
        private ButtonControl applyButton;
        private OSCInfoObject oscInfo;
        private String[] OSCLabels;
        private String[] trackingLabels = new String[] {"Default","Closest (1 Person)"
        , "Closest (2 People)", "ID (1 Person)", "ID (2 People)", "Most Active (1 Person)"
        , "Most Active (2 People)"};
        private int[] trackingLocsY;
        private int trackingLocsX = 110;

        public Game1()
        {   //set up system variables (screen width, height, content, e.t.c)
            this.IsMouseVisible = true;
            this.Window.Title = "KOSC";
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = ((width / 4) * 3) + 110;
            this.graphics.PreparingDeviceSettings += this.GraphicsDevicePreparingDeviceSettings;
            this.graphics.SynchronizeWithVerticalRetrace = true;
            this.viewPortRectangle = new Rectangle(10, 80, 400, 300);
            Content.RootDirectory = "Content";

            //create a kinect chooser and add to services
            this.chooser = new KinectChooser(this, ColorImageFormat.RgbResolution640x480Fps30);
            this.Services.AddService(typeof(KinectChooser), this.chooser);
            //create a colour stream for the kinect device
            this.colorStream = new ColorStreamRenderer(this);
            this.Components.Add(this.chooser);

            //set up GUI
            this.index = 0;
            this.input = new InputManager(Services, Window.Handle);
            this.gui = new GuiManager(Services);
            Components.Add(this.input);
            Components.Add(this.gui);
            this.gui.Visible = false;

            //create circle buttons
            this.circles = new Circle[20];
            this.circles[0] = new Circle(600, 45, 10, "Head");//head
            this.circles[1] = new Circle(600, 80, 7, "Centre Shoulder");//shoulder Centre
            this.circles[2] = new Circle(570, 92, 7, "Left Shoulder");//shoulder left
            this.circles[3] = new Circle(634, 92, 7, "Right Shoulder");//shoulder right
            this.circles[4] = new Circle(550, 140, 7, "Left Elbow");//elbow left
            this.circles[5] = new Circle(652, 140, 7, "Right Elbow");//elbow right
            this.circles[6] = new Circle(521, 180, 7, "Left Wrist");//wrist left
            this.circles[7] = new Circle(676, 180, 7, "Right Wrist");//wrist right
            this.circles[8] = new Circle(510, 200, 7, "Left Hand");//hand left
            this.circles[9] = new Circle(690, 200, 7, "Right Hand");//hand right
            this.circles[10] = new Circle(610, 195, 7, "Centre Hip");//hip centre
            this.circles[11] = new Circle(585, 195, 7, "Left Hip");//hip left
            this.circles[12] = new Circle(635, 195, 7, "Right Hip");//hip right
            this.circles[13] = new Circle(577, 270, 7, "Left Knee");//knee left
            this.circles[14] = new Circle(620, 270, 7, "Right Knee");//knee right
            this.circles[15] = new Circle(559, 350, 7, "Left Ankle");//ankle left
            this.circles[16] = new Circle(615, 350, 7, "Right Ankle");//ankle right
            this.circles[17] = new Circle(547, 375, 7, "Left Foot");//foot left
            this.circles[18] = new Circle(621, 371, 7, "Right Foot");//foot right
            this.circles[19] = new Circle(605, 140, 7, "Spine");//spine
            this.Services.AddService(typeof(Circle[]), this.circles);
            this.jointList = new List<string>();
            this.oscInfo = new OSCInfoObject();
            this.Services.AddService(typeof(OSCInfoObject), this.oscInfo);
            this.OSCLabels = new String[4];
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here       
            this.Components.Add(this.colorStream);
            for (int i = 0; i < circles.Length; i++)
            {
                this.circles[i].setupCircleEffect(this.graphics);
            }
            Screen mainScreen = new Screen(800, 200);
            this.gui.Screen = mainScreen;
            mainScreen.Desktop.Bounds = new UniRectangle(
            new UniScalar(0.0f, 0.0f), new UniScalar(0.0f, 0.0f), // x and y = 10%
            new UniScalar(1f, 0.0f), new UniScalar(1, 0.0f) // width and height = 80%
            );
            createDesktopControls(mainScreen);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this.Services.AddService(typeof(SpriteBatch), this.spriteBatch);
            this.font = Content.Load<SpriteFont>("Segoe16");
            this.labelFont = Content.Load<SpriteFont>("customFont");
            this.humanRigPicture = Content.Load<Texture2D>("Human Rig");
            //this.colorStream.Position = new Vector2(this.viewPortRectangle.X, this.viewPortRectangle.Y);
            //this.colorStream.Size = new Vector2(this.viewPortRectangle.Width, this.viewPortRectangle.Height);
            base.LoadContent();
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            this.oscList.Items.Clear();
            this.jointList.Clear();
            // this.guiManager.Update(gameTime);
            this.ms = Mouse.GetState();
            for (int i = 0; i < circles.Length; i++)
            {
                this.circles[i].mouseOver(ms.X, ms.Y);
                if (this.circles[i].getOverCircle())
                {
                    this.index = i;
                }//check whether we are over the circle if we are store which circle we are over

                if (this.circles[i].getOverCircle() && ms.LeftButton == ButtonState.Pressed)
                {
                    if (this.circles[i].GetActiveStatus() == true)
                    {
                        this.circles[i].SetActiveStatus(false);
                    }
                    else
                    {
                        this.circles[i].SetActiveStatus(true);
                    }
                }//this will set the circles colour as to whether it is sending osc messages or not

                this.circles[i].ActiveAction();

                //for the output box so that the user knows what joints are on what OSC channel, adds the active outputting joint names
                //to a list of strings, reason why we go into a list then output list is for sequential ordering of OSC channels
                if (this.circles[i].GetActiveStatus() == true)
                {
                    this.jointList.Add("Joint: " + this.circles[i].getJointName());
                }
            }

            //initialize a counter so that when joints are added to the message box of joint outputs they are accurate
            int counter = 0;
            for (int i = 0; i < this.jointList.Count; i++)
            {
                this.oscList.Items.Add(this.jointList[i] + "(X), Channel: " + (this.oscInfo.getStartChannel() + i + counter));
                this.oscList.Items.Add(this.jointList[i] + "(Y), Channel: " + (this.oscInfo.getStartChannel() + (i + 1) + counter));
                counter += 1;
            }

            //what is the current joint being scrolled over?
            currentJoint = this.circles[index].getJointName();

            //what combo box is selected? if the lightjams one is then you cannot see the custom options and the osc options are put on there
            // default settings, if custom is selected then options are presented and we will be able to choose what osc server we send to.
            if (this.lightJamsOption.Selected)
            {
                this.portInput.Bounds = new UniRectangle(0, 0, 0, 0);
                this.ipInput.Bounds = new UniRectangle(0, 0, 0, 0);
                this.startAddressInput.Bounds = new UniRectangle(0, 0, 0, 0);
                this.startMessageInput.Bounds = new UniRectangle(0, 0, 0, 0);
                this.applyButton.Bounds = new UniRectangle(0, 0, 0, 0);
                for (int i = 0; i < this.OSCLabels.Length; i++)
                {
                    this.OSCLabels[i] = "";
                }
                this.oscInfo.setPortNumber("9001");
                this.oscInfo.setStartChannelInt(1);
                this.oscInfo.setOSCIPAddressIP(IPAddress.Loopback);
                this.oscInfo.setStartString("/lj/osc/");

            }
            else if (this.customOption.Selected)
            {
                this.portInput.Bounds = new UniRectangle(625, 585, 165, 25);
                this.ipInput.Bounds = new UniRectangle(440, 585, 165, 25);
                this.startAddressInput.Bounds = new UniRectangle(440, 635, 165, 25);
                this.startMessageInput.Bounds = new UniRectangle(625, 635, 165, 25);
                this.applyButton.Bounds = new UniRectangle(575, 675, 60, 25);
                this.OSCLabels[0] = "Port Number:";
                this.OSCLabels[1] = "IP Address";
                this.OSCLabels[2] = "Start Address:";
                this.OSCLabels[3] = "Start Message:";
            }
            for (int i = 0; i < trackingControls.Length; i++)
            {
                if (trackingControls[i].Selected)
                {
                    //dependant on the control selected set the enum to the specific variable
                    switch (i)
                    {
                        case 0:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.DefaultSystemTracking;
                            break;
                        case 1:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.Closest1Player;
                            break;
                        case 2:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.Closest2Player;
                            break;
                        case 3:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.Sticky1Player;
                            break;
                        case 4:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.Sticky2Player;
                            break;
                        case 5:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.MostActive1Player;
                            break;
                        case 6:
                            this.colorStream.SkeletonStream.SkelChooser.SkeletonChooserMode = SkeletonChooserMode.MostActive2Player;
                            break;
                    }
                }
            }

            base.Update(gameTime);
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            {
                this.Exit();
            }
        }




        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            //we should always make sure that the kinect is connected before we draw any of the main application
            if (chooser.LastStatus == KinectStatus.Connected)
            {
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(humanRigPicture, new Vector2(390, 0),
                        null,
                        Color.White,
                        0,
                        new Vector2(0, 0),
                        1,
                        SpriteEffects.None,
                        0);
                this.spriteBatch.DrawString(this.font, "Kinect View", new Vector2(10, 10), Color.White);
                this.spriteBatch.DrawString(this.font, "Joint: " + this.currentJoint, new Vector2(500, 400), Color.White);
                this.spriteBatch.DrawString(this.labelFont, "OSC Options", new Vector2(440, 515), Color.White);
                this.spriteBatch.DrawString(this.labelFont, "LightJams", new Vector2(460, 540), Color.White);
                this.spriteBatch.DrawString(this.labelFont, "Custom", new Vector2(565, 540), Color.White);
                this.spriteBatch.DrawString(this.font, "Kinect Options:", new Vector2(10, 340), Color.White);
                this.spriteBatch.DrawString(this.labelFont, this.OSCLabels[0], new Vector2(625, 570), Color.White);
                this.spriteBatch.DrawString(this.labelFont, this.OSCLabels[1], new Vector2(440, 570), Color.White);
                this.spriteBatch.DrawString(this.labelFont, this.OSCLabels[2], new Vector2(440, 620), Color.White);
                this.spriteBatch.DrawString(this.labelFont, this.OSCLabels[3], new Vector2(625, 620), Color.White);
                this.spriteBatch.DrawString(this.labelFont, "Tracking Mode:", new Vector2(10, 439), Color.White);
                this.spriteBatch.DrawString(this.labelFont, "Tilt:", new Vector2(10, 393), Color.White);
                for (int i = 0; i < trackingLabels.Length; i++)
                {
                    this.spriteBatch.DrawString(this.labelFont, this.trackingLabels[i], new Vector2(trackingLocsX + 20, trackingLocsY[i]), Color.White);
                }
                this.spriteBatch.End();
                this.gui.Draw(gameTime);
                for (int i = 0; i < circles.Length; i++)
                {
                    this.circles[i].drawCircle(this.graphics);
                }
                this.colorStream.Visible = true;
            }
            else
            {
                this.colorStream.Visible = false;

            }
            base.Draw(gameTime);
        }

        /// <summary>
        /// This method ensures that we can render to the back buffer without
        /// losing the data we already had in our previous back buffer.  This
        /// is necessary for the SkeletonStreamRenderer.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event args.</param>
        private void GraphicsDevicePreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            // This is necessary because we are rendering to back buffer/render targets and we need to preserve the data
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        /// <summary>
        /// This method initialized all the GUI controls from the nucelx library
        /// could have been done in the initialize method but is called from there
        /// instead for logic and space seperation.
        /// </summary>
        private void createDesktopControls(Screen mainScreen)
        {
            this.oscList = new ListControl();
            this.oscList.Bounds = new UniRectangle(440.0f, 430.0f, 350.0f, 80.0f);
            this.oscList.Slider.Bounds.Location.X.Offset -= 1.0f;
            this.oscList.Slider.Bounds.Location.Y.Offset += 1.0f;
            this.oscList.Slider.Bounds.Size.Y.Offset -= 2.0f;
            this.oscList.SelectionMode = ListSelectionMode.Single;
            mainScreen.Desktop.Children.Add(this.oscList);

            this.portInput = new InputControl();
            this.portInput.Bounds = new UniRectangle(10, 40, 50, 25);
            mainScreen.Desktop.Children.Add(this.portInput);

            this.ipInput = new InputControl();
            this.ipInput.Bounds = new UniRectangle(10, 470, 50, 25);
            mainScreen.Desktop.Children.Add(this.ipInput);

            this.startAddressInput = new InputControl();
            this.startAddressInput.Bounds = new UniRectangle(10, 530, 50, 25);
            mainScreen.Desktop.Children.Add(this.startAddressInput);

            this.startMessageInput = new InputControl();
            this.startMessageInput.Bounds = new UniRectangle(10, 600, 50, 25);
            mainScreen.Desktop.Children.Add(this.startMessageInput);

            this.lightJamsOption = new ChoiceControl();
            this.lightJamsOption.Bounds = new UniRectangle(440, 540, 60, 16);
            this.lightJamsOption.Selected = true;
            mainScreen.Desktop.Children.Add(this.lightJamsOption);

            this.customOption = new ChoiceControl();
            this.customOption.Bounds = new UniRectangle(545, 540, 70, 16);
            mainScreen.Desktop.Children.Add(this.customOption);

            this.applyButton = new ButtonControl();
            this.applyButton.Bounds = new UniRectangle(0, 0, 0, 0);
            this.applyButton.Text = "Apply";
            this.applyButton.Pressed += new EventHandler(applyClicked);
            mainScreen.Desktop.Children.Add(this.applyButton);

            //initialize the kinect controls and locations
            this.kinectTiltUp = new ButtonControl();
            this.kinectTiltUp.Bounds = new UniRectangle(40, 390, 30, 25);
            this.kinectTiltUp.Text = "+";
            this.kinectTiltUp.Pressed += new EventHandler(kinectTiltClicked);
            mainScreen.Desktop.Children.Add(this.kinectTiltUp);

            this.kinectTiltDown = new ButtonControl();
            this.kinectTiltDown.Bounds = new UniRectangle(80, 390, 30, 25);
            this.kinectTiltDown.Text = "-";
            this.kinectTiltDown.Pressed += new EventHandler(kinectTiltClicked);
            mainScreen.Desktop.Children.Add(this.kinectTiltDown);

            trackingLocsY = new int[7];

            for (int i = 0; i < 7; i++)
            {
                trackingLocsY[i] = 440 + (i * 30);
            }

            trackingControls = new ChoiceControl[7];
            for (int i = 0; i < trackingControls.Length; i++)
            {
                trackingControls[i] = new ChoiceControl();
                trackingControls[i].Bounds = new UniRectangle(trackingLocsX, trackingLocsY[i], 60, 16);
                if (i == 0)
                {
                    trackingControls[i].Selected = true;
                }
                mainScreen.Desktop.Children.Add(trackingControls[i]);
            }
        }

        /// <summary>
        /// When apply is clicked set the osc information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void applyClicked(object sender, EventArgs arguments)
        {
            this.oscInfo.setOSCIPAddress(this.ipInput.Text);
            this.oscInfo.setPortNumber(this.portInput.Text);
            this.oscInfo.setStartString(this.startMessageInput.Text);
            this.oscInfo.setStartChannel(this.startAddressInput.Text);
        }

        /// <summary>
        /// When the kinect tilt button is pressed we move the kinect camera up or down
        /// dependant on the button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void kinectTiltClicked(object sender, EventArgs arguments)
        {
            // Check for input to rotate the camera up and down around the model.
            if (sender == kinectTiltDown)
            {
                
            }
            else
            {
               
            }
        }
    }
}