import core.KotlinRunner
import core.RunnerServer

fun main() {
    println("[Main] Starting Kotlin runner…")
    val runner = KotlinRunner()
    val server = RunnerServer(runner)
    println("[Main] About to start server on port 5000")
    server.start(port = 5000)
    println("[Main] Server started") // возможно никогда не дойдет сюда
}

