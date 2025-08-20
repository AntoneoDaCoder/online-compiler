package enums

enum class ExecutionStatus(val code: Int) {
    NoStatus(0),
    Succeded(1),
    CompileError(2),
    RuntimeError(3),
    TimedOut(4),
    Cancelled(5),
    FailedToExecute(6),
    Pending(7)
}