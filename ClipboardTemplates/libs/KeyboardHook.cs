using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


///
/// Allows you to hook shortcuts globally
/// @link http://www.liensberger.it/web/blog/?keyString=207
/// 
 
namespace ClipboardTemplates
{
    public sealed class KeyboardHook : IDisposable
    {
        // Registers a hot valueField with Windows.
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot valueField with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);


        /// <summary>
        /// Represents the window that is used internally to get the messages.
        /// </summary>
        private class Window : NativeWindow, IDisposable
        {
            private static int WM_HOTKEY = 0x0312;
    
            public Window()
            {
                // create the handle for the window.
                this.CreateHandle(new CreateParams());
            }
    
            /// <summary>
            /// Overridden to get the notifications.
            /// </summary>
            /// <param name="m"></param>
            protected override void WndProc(ref Message m)
            {
                
    
                // check if we got a hot valueField pressed.
                if (m.Msg == WM_HOTKEY)
                {
                    // get the keys.
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    ModifierKeys modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    if (KeyPressed != null)
                        KeyPressed(this, new KeyPressedEventArgs(modifier, key));
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
    
            public event EventHandler<KeyPressedEventArgs> KeyPressed;
    
            #region IDisposable Members
    
            public void Dispose()
            {
                this.DestroyHandle();
            }
    
            #endregion
        }


    
        private Window _window = new Window();
        private List<KeyboardHook_item> _hooks = new List<KeyboardHook_item>();
        private int _currentId = 1000; // just some number
    
        public KeyboardHook()
        {
            // register the event of the inner native window.
            _window.KeyPressed += delegate(object sender, KeyPressedEventArgs args)
            {
                var i = getKeyboardHookItem(args.Modifier, args.Key);

                if (KeyPressed != null)
                    KeyPressed(i, args);
                
                if (i != null) i.onKeyPressed(args);
            };
        }

        /// <summary>
        /// Getts keyboard item for given modifier and key
        /// If not registered, null is returned
        /// </summary>
        /// <param name="m"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public KeyboardHook_item getKeyboardHookItem(ModifierKeys m, Keys k) {
            foreach (var h in this._hooks)
                if (h.Modifier == m && h.Key == k)
                    return h;
            return null;
        }
    
        /// <summary>
        /// Registers a hot valueField in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot valueField.</param>
        /// <param name="valueField">The valueField itself that is associated with the hot valueField.</param>
        public KeyboardHook_item RegisterHotKey(ModifierKeys modifier, Keys key)
        {
            var id = _currentId++;

            // register the hot valueField.
            if (!RegisterHotKey(_window.Handle, id, (uint)modifier, (uint)key))
                throw new InvalidOperationException("Couldn’t register the hot key.");

            var h = new KeyboardHook_item(modifier, key, id, this);
            _hooks.Add(h);
            return h;
        }

        /// <summary>
        /// Shortcut for registering hotkey with one event listener
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="key"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public KeyboardHook_item RegisterHotKey(ModifierKeys modifier, Keys key, EventHandler<KeyPressedEventArgs> e) {
            var a = this.RegisterHotKey(modifier, key);
            a.KeyPressed += e;
            return a;
        }

        /// <summary>
        /// Unregisters a hotkey
        /// </summary>
        /// <param name="h"></param>
        public void UnregisterHotKey(KeyboardHook_item h) {
            UnregisterHotKey(_window.Handle, h.Id);
            this._hooks.Remove(h);
        }

        /// <summary>
        /// Unregisters all hooks
        /// </summary>
        public void UnregisterAll() {
            // (.ToArray() is little hack to avoid this exception: http://stackoverflow.com/questions/604831/collection-was-modified-enumeration-operation-may-not-execute
            // C# do not have support for traversing list and modifing it simultanously

            foreach(var hook in this._hooks.ToArray()) {
                hook.Unregister();
            };
        }
    
        /// <summary>
        /// A hot valueField has been pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;
    
        #region IDisposable Members
    
        public void Dispose()
        {
            // unregister all the registered hot keys.
            UnregisterAll();
    
            // dispose the inner native window.
            _window.Dispose();
        }
    
        #endregion
    }

    public class KeyboardHook_item {
        public event EventHandler<KeyPressedEventArgs> KeyPressed = delegate{ };

        private ModifierKeys modifier;
        public ModifierKeys Modifier
        {
            get { return modifier; }
            protected set { modifier = value; }
        }

        private Keys key;
        public Keys Key
        {
            get { return key; }
            protected set { key = value; }
        }

        private int id;
        public int Id
        {
            get { return id; }
            protected set { id = value; }
        }

        private KeyboardHook keyboardHookController;
        public KeyboardHook KeyboardHookController
        {
            get { return keyboardHookController; }
            protected set { keyboardHookController = value; }
        }

        public object CustomData;

        /// <summary>
        /// Internal
        /// </summary>
        /// <param name="args"></param>
        public void onKeyPressed(KeyPressedEventArgs args){
            this.KeyPressed(this, args);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="m"></param>
        /// <param name="k"></param>
        /// <param name="id"></param>
        public KeyboardHook_item(ModifierKeys m, Keys k, int id, KeyboardHook kh)
        {
            this.Modifier = m;
            this.Key = k;
            this.Id = id;
            this.KeyboardHookController = kh;
        }


        public void Unregister()
        {
            this.KeyboardHookController.UnregisterHotKey(this);
        }
    }
    
    /// <summary>
    /// Event Args for the event that is fired after the hot valueField has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        private ModifierKeys _modifier;
        private Keys _key;

        internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
        {
            _modifier = modifier;
            _key = key;
        }
    
        public ModifierKeys Modifier
        {
            get { return _modifier; }
        }
    
        public Keys Key
        {
            get { return _key; }
        }

        


    }
    
    /// <summary>
    /// The enumeration of possible modifiers.
    /// </summary>
    [Flags]
    public enum ModifierKeys : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}
