const { execSync } = require("child_process")
const fs = require("fs")
const path = require("path")

const projectPath = "dotnet_server/ChattyMcChatface.Api"
// Output file will be in the same directory as the script (project root)
const outputFilePath = path.join(__dirname, "appsettings.Docker.json")

try {
    // 1. Execute dotnet user-secrets list
    const command = `dotnet user-secrets list --project ${projectPath}`
    console.log(`Executing: ${command}`)
    // Ensure the command is executed from the project root where the script lives
    const output = execSync(command, { encoding: "utf8", cwd: __dirname })
    console.log("Raw secrets output received.") // Don't log raw secrets themselves

    // 2. Parse the output (Key = Value format)
    const secrets = {}
    const lines = output.trim().split("\n")

    lines.forEach((line) => {
        const separatorIndex = line.indexOf(" = ")
        if (separatorIndex > 0) {
            const key = line.substring(0, separatorIndex).trim()
            const value = line.substring(separatorIndex + 3).trim()

            // 3. Reconstruct hierarchical JSON
            const keys = key.split(":")
            let currentLevel = secrets
            keys.forEach((k, index) => {
                if (index === keys.length - 1) {
                    // Last key, assign value
                    // Attempt to parse value as JSON if it looks like it (e.g., for VertexAI:KeyJsonContent)
                    try {
                        // A simple check: does it start with { and end with }?
                        if (value.startsWith("{") && value.endsWith("}")) {
                            currentLevel[k] = JSON.parse(value)
                        } else {
                            currentLevel[k] = value // Assign as string otherwise
                        }
                    } catch (e) {
                        // If JSON parsing fails, assign as string
                        currentLevel[k] = value
                    }
                } else {
                    // Create nested object if it doesn't exist
                    if (!currentLevel[k] || typeof currentLevel[k] !== "object") {
                        currentLevel[k] = {}
                    }
                    currentLevel = currentLevel[k]
                }
            })
        } else if (line.trim() && !line.includes("No secrets configured")) {
            // Ignore empty lines and the 'no secrets' message
            console.warn(`Skipping malformed line: ${line}`)
        }
    })

    // 4. Write to appsettings.Docker.json
    const jsonContent = JSON.stringify(secrets, null, 2) // Pretty print
    console.log(`\nWriting generated configuration structure to ${outputFilePath}`)
    fs.writeFileSync(outputFilePath, jsonContent)

    console.log(`\nSuccessfully generated ${outputFilePath}`)
} catch (error) {
    console.error(`Error generating secrets file: ${error.message}`)
    if (error.stderr) {
        console.error(`stderr: ${error.stderr}`)
    }
    // Avoid logging stdout on error as it might contain secrets if parsing failed mid-way
    process.exit(1)
}
