using System;
using System.Threading.Tasks;

namespace SubApp.Scripts;

public record OpenOrCloseConfirmDelete(Func<Task>? DeleteAction = null);