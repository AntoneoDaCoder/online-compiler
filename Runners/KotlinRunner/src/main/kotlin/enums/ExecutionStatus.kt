package enums
import kotlinx.serialization.Serializable

@Serializable
enum class ExecutionStatus(val code: Int) {
    NoStatus(0),
    Succeeded(1),
    CompileError(2),
    RuntimeError(3),
    TimedOut(4),
    Cancelled(5),
    FailedToExecute(6),
    Pending(7)
}