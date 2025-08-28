package enums
import kotlinx.serialization.Serializable

@Serializable
enum class RequestStatus (val code:Int){
    NoStatus (0),
    Acknowledged(1),
    Executing (2),
    Failed (3),
    Succeeded (4),
    Cancelled(5)
}