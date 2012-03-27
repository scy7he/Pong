using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Jitter;
using Jitter.Dynamics;
using Jitter.Dynamics.Constraints;
using Jitter.Dynamics.Joints;
using Jitter.Collision;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
using Jitter.Dynamics.Constraints;
using Jitter.Dynamics.Joints;
using System.Reflection;
using System.Diagnostics;

namespace Pong
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        ContentManager content;

        CollisionSystem collisionSystem;
        World world;

        public Camera Camera { private set; get; }
        public Display Display { private set; get; }

        private Color backgroundColor = new Color(63, 66, 73);
        public BasicEffect BasicEffect { private set; get; }

        private int activeBodies = 0;

        private enum Primitives { box, sphere, cylinder, cone, capsule }

        private GeometricPrimitive[] primitives =
            new GeometricPrimitive[5];

        RasterizerState wireframe, cullMode, normal;

        Player player;

        public DebugDrawer DebugDrawer { private set; get; }
        Color[] rndColors;

        RigidBody ball;
        RigidBody wall1, wall2;



        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            collisionSystem = new CollisionSystemPersistentSAP();
            collisionSystem.CollisionDetected += new CollisionDetectedHandler(CollisionDetected);
            
            world = new World(collisionSystem);

            Random rr = new Random();
            rndColors = new Color[20];
            for (int i = 0; i < 20; i++)
            {
                rndColors[i] = new Color((float)rr.NextDouble(), (float)rr.NextDouble(), (float)rr.NextDouble());
            }

            wireframe = new RasterizerState();
            wireframe.FillMode = FillMode.WireFrame;

            cullMode = new RasterizerState();
            cullMode.CullMode = CullMode.None;

            normal = new RasterizerState();

           
        }

        private void CollisionDetected(RigidBody body1, RigidBody body2, JVector point1, JVector point2, JVector normal, float penetration)
        {
            // here i add the collision bodies to a list because what comes in as body1 and body2 varies from collision to collision
            List<RigidBody> collisionObjects = new List<RigidBody>(2);
            collisionObjects.Add(body1);
            collisionObjects.Add(body2);

            if (collisionObjects.Contains(ball) && (collisionObjects.Contains(wall1) || collisionObjects.Contains(wall2)))
            {
                ball.LinearVelocity = ball.LinearVelocity * 2f;
            }
                
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Camera = new Camera(this);
            Camera.Position = new Vector3(35f, 20f, 0f);
            Camera.Target = Camera.Position + Vector3.Normalize(new Vector3(7, 4, 0));
            this.Components.Add(Camera);

            Display = new Display(this);
            Display.DrawOrder = int.MaxValue;
            this.Components.Add(Display);

            DebugDrawer = new DebugDrawer(this);
            this.Components.Add(DebugDrawer);

            primitives[(int)Primitives.box] = new BoxPrimitive(GraphicsDevice);
            primitives[(int)Primitives.capsule] = new CapsulePrimitive(GraphicsDevice);
            primitives[(int)Primitives.cone] = new ConePrimitive(GraphicsDevice);
            primitives[(int)Primitives.cylinder] = new CylinderPrimitive(GraphicsDevice);
            primitives[(int)Primitives.sphere] = new SpherePrimitive(GraphicsDevice);

            BasicEffect = new BasicEffect(GraphicsDevice);
            BasicEffect.EnableDefaultLighting();
            BasicEffect.PreferPerPixelLighting = true;

            base.Initialize();
        }

        

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            if (content == null)
                content = new ContentManager(this.Services, "Content");
            
            player = new Player();

            world.AddBody(player.body);


            // ball
            JVector position = new JVector(0f, 5f, -10f);
            Shape sphereShape = new SphereShape(1.0f);
            ball = new RigidBody(sphereShape);
            ball.Position = position;
            ball.Material.Restitution = 2.0f;
            ball.Mass = 10f;
            ball.IsStatic = true;


            // floor
            Shape boxShape = new BoxShape(new JVector(20f, 1.5f, 40f));
            RigidBody floor = new RigidBody(boxShape);
            floor.Position = JVector.Zero;
            floor.IsStatic = true;
            
            Shape box = new BoxShape(new JVector(20f, 20f, 1.5f));
            wall1 = new RigidBody(box);
            wall2 = new RigidBody(box);
            wall1.Position = JVector.Zero - new JVector(0f,0f,20f);
            wall2.Position = JVector.Zero + new JVector(0f, 0f, 20f);
            wall1.IsStatic = true;
            wall2.IsStatic = true;


            world.AddBody(wall1);
            world.AddBody(wall2);
            world.AddBody(ball);
            world.AddBody(floor);

           
        }

        public void ExtractData(List<Vector3> vertices, List<TriangleVertexIndices> indices, Model model)
        {
            Matrix[] bones_ = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(bones_);
            foreach (ModelMesh mm in model.Meshes)
            {
                Matrix xform = bones_[mm.ParentBone.Index];
                foreach (ModelMeshPart mmp in mm.MeshParts)
                {
                    int offset = vertices.Count;
                    Vector3[] a = new Vector3[mmp.NumVertices];
                    mmp.VertexBuffer.GetData<Vector3>(mmp.VertexOffset * mmp.VertexBuffer.VertexDeclaration.VertexStride,
                        a, 0, mmp.NumVertices, mmp.VertexBuffer.VertexDeclaration.VertexStride);
                    for (int i = 0; i != a.Length; ++i)
                        Vector3.Transform(ref a[i], ref xform, out a[i]);
                    vertices.AddRange(a);

                    if (mmp.IndexBuffer.IndexElementSize != IndexElementSize.SixteenBits)
                        throw new Exception(
                            String.Format("Model uses 32-bit indices, which are not supported."));
                    short[] s = new short[mmp.PrimitiveCount * 3];
                    mmp.IndexBuffer.GetData<short>(mmp.StartIndex * 2, s, 0, mmp.PrimitiveCount * 3);
                    TriangleVertexIndices[] tvi = new TriangleVertexIndices[mmp.PrimitiveCount];
                    for (int i = 0; i != tvi.Length; ++i)
                    {
                        tvi[i].I0 = s[i * 3 + 0] + offset;
                        tvi[i].I1 = s[i * 3 + 1] + offset;
                        tvi[i].I2 = s[i * 3 + 2] + offset;
                    }
                    indices.AddRange(tvi);
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            
            
            KeyboardState keys = Keyboard.GetState();
           


            if (keys.IsKeyDown(Keys.Up))
            {
                ball.AddForce(JVector.Forward * 100f);
            }

            if (keys.IsKeyDown(Keys.Left))
            {
                ball.AddForce(JVector.Left * 100f);
            }

            if (keys.IsKeyDown(Keys.Right))
            {
                ball.AddForce(JVector.Right * 100f);
            }

            if (keys.IsKeyDown(Keys.Down))
            {
                ball.AddForce(JVector.Backward * 100f);
            }

            if (keys.IsKeyDown(Keys.Space))
            {
                ball.AddForce(JVector.Up * 100f);
            }

            if (keys.IsKeyDown(Keys.R))
            {
                ball.IsStatic = false;
                ball.LinearVelocity = JVector.Zero;
                ball.Position = new JVector(0f, 9f, 18f);
                ball.AddForce(new JVector(0f, 1000f, -10000f));
            }

                                    
            //player.HandleInput(gameTime);

            world.Step(1.0f / 50.0f, true);

            base.Update(gameTime);
        }

       
        private void AddShapeToDrawList(Shape shape, JMatrix ori, JVector pos)
        {
            GeometricPrimitive primitive = null;
            Matrix scaleMatrix = Matrix.Identity;

            if (shape is BoxShape)
            {
                primitive = primitives[(int)Primitives.box];
                scaleMatrix = Matrix.CreateScale(Conversion.ToXNAVector((shape as BoxShape).Size));
            }
            else if (shape is SphereShape)
            {
                primitive = primitives[(int)Primitives.sphere];
                scaleMatrix = Matrix.CreateScale((shape as SphereShape).Radius);
            }
            else if (shape is CylinderShape)
            {
                primitive = primitives[(int)Primitives.cylinder];
                CylinderShape cs = shape as CylinderShape;
                scaleMatrix = Matrix.CreateScale(cs.Radius, cs.Height, cs.Radius);
            }
            else if (shape is CapsuleShape)
            {
                primitive = primitives[(int)Primitives.capsule];
                CapsuleShape cs = shape as CapsuleShape;
                scaleMatrix = Matrix.CreateScale(cs.Radius * 2, cs.Length, cs.Radius * 2);

            }
            else if (shape is ConeShape)
            {
                ConeShape cs = shape as ConeShape;
                scaleMatrix = Matrix.CreateScale(cs.Radius, cs.Height, cs.Radius);
                primitive = primitives[(int)Primitives.cone];
            }

            if (primitive != null)
                primitive.AddWorldMatrix(scaleMatrix * Conversion.ToXNAMatrix(ori) *
                            Matrix.CreateTranslation(Conversion.ToXNAVector(pos)));
        }

        private void AddBodyToDrawList(RigidBody rb)
        {
            //if (rb.Tag is BodyTag && ((BodyTag)rb.Tag) == BodyTag.DontDrawMe) return;

            bool isCompoundShape = (rb.Shape is CompoundShape);

            if (!isCompoundShape)
            {
                AddShapeToDrawList(rb.Shape, rb.Orientation, rb.Position);
            }
            else
            {
                CompoundShape cShape = rb.Shape as CompoundShape;
                JMatrix orientation = rb.Orientation;
                JVector position = rb.Position;

                foreach (var ts in cShape.Shapes)
                {
                    JVector pos = ts.Position;
                    JMatrix ori = ts.Orientation;

                    JVector.Transform(ref pos, ref orientation, out pos);
                    JVector.Add(ref pos, ref position, out pos);

                    JMatrix.Multiply(ref ori, ref orientation, out ori);

                    AddShapeToDrawList(ts.Shape, ori, pos);
                }

            }

        }

        private void DrawJitterDebugInfo()
        {
            int cc = 0;

            foreach (Constraint constr in world.Constraints)
                constr.DebugDraw(DebugDrawer);

            foreach (RigidBody body in world.RigidBodies)
            {
                DebugDrawer.Color = rndColors[cc % rndColors.Length];
                body.DebugDraw(DebugDrawer);
                cc++;
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(backgroundColor);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            BasicEffect.View = Camera.View;
            BasicEffect.Projection = Camera.Projection;

            activeBodies = 0;

            // Draw all shapes
            foreach (RigidBody body in world.RigidBodies)
            {
                if (body.IsActive) activeBodies++;
                if (body.Tag is int || body.IsParticle) continue;
                AddBodyToDrawList(body);
                
            }

             // drawing all contacts!
            foreach (Arbiter a in world.ArbiterMap)
            {
                foreach (Contact c in a.ContactList)
               {
                    DebugDrawer.DrawLine(c.Position1 + 0.5f * JVector.Left, c.Position1 + 0.5f * JVector.Right, Color.Green);
                    DebugDrawer.DrawLine(c.Position1 + 0.5f * JVector.Up, c.Position1 + 0.5f * JVector.Down, Color.Green);
                    DebugDrawer.DrawLine(c.Position1 + 0.5f * JVector.Forward, c.Position1 + 0.5f * JVector.Backward, Color.Green);


                    DebugDrawer.DrawLine(c.Position2 + 0.5f * JVector.Left, c.Position2 + 0.5f * JVector.Right, Color.Red);
                    DebugDrawer.DrawLine(c.Position2 + 0.5f * JVector.Up, c.Position2 + 0.5f * JVector.Down, Color.Red);
                    DebugDrawer.DrawLine(c.Position2 + 0.5f * JVector.Forward, c.Position2 + 0.5f * JVector.Backward, Color.Red);
                }
            }
             
            

            // draw all debug shapes
            DrawJitterDebugInfo();

            BasicEffect.DiffuseColor = Color.LightGray.ToVector3();

            foreach (GeometricPrimitive prim in primitives)
                prim.Draw(BasicEffect);

            GraphicsDevice.RasterizerState = cullMode;
            
            base.Draw(gameTime);

            GraphicsDevice.RasterizerState = normal;


        }

        void DrawModel(Model model, Matrix worldMatrix)
        {
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = worldMatrix;
                    effect.View = Camera.View; //camera.View;
                    effect.Projection = Camera.Projection; // camera.Projection;
                    effect.TextureEnabled = true;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;

                    // Set the fog to match the black background color
                    effect.FogEnabled = true;
                    effect.FogColor = Vector3.Zero;
                    effect.FogStart = 1000;
                    effect.FogEnd = 5000;
                }

                mesh.Draw();
            }
        }
    }
}
