import core.KotlinRunner
import core.RunnerServer

fun main() {
    val runner = KotlinRunner()
    val server = RunnerServer(runner)

    // сервер сам держит процесс пока не убьёшь SIGTERM
    server.start(port = 5000)
}
