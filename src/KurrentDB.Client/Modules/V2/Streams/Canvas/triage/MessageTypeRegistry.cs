// using System.Runtime.CompilerServices;
// using JetBrains.Annotations;
// using KurrentDB.Client.Model;
//
// namespace KurrentDB.Client.SchemaRegistry;
//
// [PublicAPI]
// public class MessageTypeRegistry {
//     readonly Dictionary<(Type MessageType, SchemaDataFormat SchemaType), string> _subjectsByType = new();
//     readonly Dictionary<(string Subject, SchemaDataFormat SchemaType), Type>     _typesBySubject = new();
//
//     public IReadOnlyDictionary<(Type MessageType, SchemaDataFormat SchemaType), string> SubjectsByType => _subjectsByType;
//     public IReadOnlyDictionary<(string Subject, SchemaDataFormat SchemaType), Type>     TypesBySubject => _typesBySubject;
//
//     [PublicAPI]
//     [MethodImpl(MethodImplOptions.Synchronized)]
//     public bool TryRegister(Type messageType, string subject, SchemaDataFormat schemaType) {
//         if (_subjectsByType.TryGetValue((messageType, schemaType), out var registeredSubject))
//             return registeredSubject.Equals(subject, StringComparison.Ordinal);
//
//         _subjectsByType[(messageType, schemaType)] = subject;
//         _typesBySubject[(subject, schemaType)]     = messageType;
//
//         return true;
//     }
//
//     [PublicAPI]
//     [MethodImpl(MethodImplOptions.Synchronized)]
//     public void Register(Type messageType, string subject, SchemaDataFormat schemaType) {
//         if (_subjectsByType.TryGetValue((messageType, schemaType), out var registeredSubject)) {
//             if (!registeredSubject.Equals(subject, StringComparison.Ordinal))
//                 throw new ArgumentException($"Message Type {messageType.FullName} ({schemaType}) is already registered as subject {registeredSubject}", nameof(subject));
//
//             return;
//         }
//
//         _subjectsByType[(messageType, schemaType)] = subject;
//         _typesBySubject[(subject, schemaType)]     = messageType;
//     }
//
//     // public bool TryGetMessageType(string subject, SchemaDataFormat schemaType, [MaybeNullWhen(false)] out Type messageType) =>
//     //     _typesBySubject.TryGetValue((subject, schemaType), out messageType);
//     //
//     // public bool TryGetSubject(Type messageType, SchemaDataFormat schemaType, [MaybeNullWhen(false)] out string subject) =>
//     //     _subjectsByType.TryGetValue((messageType, schemaType), out subject);
//
//     public bool TryGetMessageType(string subject, SchemaDataFormat schemaType, out Type messageType) =>
// 	    _typesBySubject.TryGetValue((subject, schemaType), out messageType);
//
//     public bool TryGetSubject(Type messageType, SchemaDataFormat schemaType,  out string subject) =>
// 	    _subjectsByType.TryGetValue((messageType, schemaType), out subject);
//
//     public Type? GetMessageType(string subject, SchemaDataFormat schemaType, bool throwWhenMissing = true) {
//         if (_typesBySubject.TryGetValue((subject, schemaType), out var messageType))
//             return messageType;
//
//         return throwWhenMissing ? throw new UnregisteredMessageTypeException(subject) : null;
//     }
//
//     public string? GetSubject(Type messageType, SchemaDataFormat schemaType, bool throwWhenMissing = true) {
//         if (_subjectsByType.TryGetValue((messageType, schemaType), out var subject))
//             return subject;
//
//         return throwWhenMissing ? throw new UnregisteredMessageTypeException(messageType) : null;
//     }
//
//     public bool IsMessageTypeRegistered(Type messageType, SchemaDataFormat schemaType) =>
//         _subjectsByType.ContainsKey((messageType, schemaType));
//
//     public bool IsSubjectRegistered(string subject, SchemaDataFormat schemaType) =>
//         _typesBySubject.ContainsKey((subject, schemaType));
// }
//
// public class UnregisteredMessageTypeException : Exception {
//     public UnregisteredMessageTypeException(Type type) : base($"Message type {type.Name} registration not found") { }
//
//     public UnregisteredMessageTypeException(string subject) : base($"Subject {subject} registration not found") { }
// }
