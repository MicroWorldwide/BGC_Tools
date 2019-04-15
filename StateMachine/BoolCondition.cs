﻿using System;

namespace BGC.StateMachine
{
    /// <summary>
    /// A bool condition checks for booleans, similar to the trigger condition,
    /// but will not consume the boolean once it has been used. Instead it keeps
    /// the value exactly as it was when a transition occurs.
    /// </summary>
    public class BoolCondition : TransitionCondition
    {
        /// <summary>
        /// Key to access required boolean in state machine
        /// </summary>
        private readonly string key;

        /// <summary>
        /// Expected boolean value for when this condition should call for a
        /// transition
        /// </summary>
        private readonly bool val;

        /// <summary>
        /// Build a boolean condition that checks the state machine boolean 
        /// dictionary and will call for a transition when the expected value
        /// is found
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public BoolCondition(string key, bool val)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(
                    paramName: nameof(key),
                    message: "bool transition cannot receive null or empty key string");
            }

            this.key = key;
            this.val = val;
        }

        /// <summary>
        /// Not used
        /// </summary>
        public override void OnTransition()
        {
            // pass
        }

        /// <summary>
        /// Returns true when the correct value specified during construction 
        /// is seen
        /// </summary>
        /// <returns></returns>
        public override bool ShouldTransition()
        {
            return getBool(key) == val;
        }

        /// <summary>
        /// Not used
        /// </summary>
        protected override void StateMachineFunctionsSet()
        {
            // pass
        }
    }
}