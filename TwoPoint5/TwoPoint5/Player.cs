using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Jitter;
using Jitter.Dynamics;
using Jitter.Collision;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
using Jitter.Dynamics.Constraints;
using Jitter.Dynamics.Joints;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;


namespace Pong
{
    class Player
    {
        public JVector position;
        public JVector forward;
        public float facing;
        float speed = 0.005f;

        public RigidBody body;
        public Model model;

        float currentTime = 0f;

        public Player()
        {
            Shape shape = new BoxShape(new JVector(1f, 0.3f, 2f));
            //Shape shape = new BoxShape(new JVector(2f, 2f, 1f));
            body = new RigidBody(shape);

            position = new JVector(0, 1.5f, -1f);

            body.Position = position;
            //body.IsStatic = true;

            facing = MathHelper.ToRadians((float)Math.PI/2);

            
        }

        public void Update(GameTime gameTime)
        {
            this.position = this.body.Position;
        }

        public void HandleInput(GameTime gameTime)
        {
            currentTime += (float)gameTime.ElapsedGameTime.Milliseconds;
            
            KeyboardState keys = Keyboard.GetState();

            if (keys.IsKeyDown(Keys.Up))
            {
                float x = (float)Math.Sin(facing);
                float z = (float)Math.Cos(facing);

                JVector newPath = new JVector(x, 0f, z) * speed;
                newPath = newPath * currentTime;
                newPath.Normalize();

                forward = newPath;

                body.AddForce(newPath*100f);
                //position = body.Position;
                //position += newPath;

                //body.Position = position;
            }

            if (keys.IsKeyDown(Keys.Left))
            {
                facing -= 0.05f;
            }

            if (keys.IsKeyDown(Keys.Right))
            {
                facing += 0.05f;
            }

            if (keys.IsKeyDown(Keys.Down))
            {
                float x = (float)Math.Sin(facing);
                float z = (float)Math.Cos(facing);

                JVector newPath = new JVector(x, 0f, z) * speed;
                newPath = newPath * currentTime;
                newPath.Normalize();

                body.AddForce(newPath * -100f);
            }

            if (keys.IsKeyDown(Keys.Space))
            {
                body.AddForce(JVector.Up * 100f);
            }
            
            /*if (keys.IsKeyDown(Keys.A))
            
            if (keys.IsKeyDown(Keys.S))
            
            if (keys.IsKeyDown(Keys.W))
            */
        }

       
    }
}
