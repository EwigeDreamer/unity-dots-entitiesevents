using System;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Attribute used to mark an assembly as containing a specific event type.
    /// The source generator will create an event system for each registered type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class RegisterEventAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterEventAttribute"/> class.
        /// </summary>
        /// <param name="eventType">The type of event to register.</param>
        public RegisterEventAttribute(Type eventType) { }
    }
}