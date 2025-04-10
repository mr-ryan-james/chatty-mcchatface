function parseSecretsOutput(output) {
    const secrets = {}
    const lines = output.trim().split("\n")
    let currentKey = null
    let currentValueLines = []

    function processAndStoreValue(targetObj, keyPath, valueString) {
        const keys = keyPath.split(":")
        let currentLevel = targetObj
        keys.forEach((k, index) => {
            if (index === keys.length - 1) {
                // Last key, assign value
                let finalValue = valueString
                // Check for JSON object/array structure
                try {
                    finalValue = JSON.parse(valueString.trim())
                    console.log(`Successfully parsed JSON for key "${keyPath}"`)
                } catch (e) {
                    console.warn(
                        `Value for key "${keyPath}" looks like JSON but failed to parse: ${e.message}. Storing as string.`,
                    )
                    finalValue = valueString // Store raw string if parsing fails
                }
                currentLevel[k] = finalValue
            } else {
                // Create nested object if it doesn't exist
                if (!currentLevel[k] || typeof currentLevel[k] !== "object") {
                    currentLevel[k] = {}
                }
                currentLevel = currentLevel[k]
            }
        })
    }

    for (const line of lines) {
        // Use regex to find lines starting a key-value pair (Key = Value)
        // Allows for spaces around '=' and captures key and first line of value
        const match = line.match(/^([^\s=][^=]*?)\s*=\s*(.*)/)

        if (match) {
            // Found a new key. Process the previous key/value if any.
            if (currentKey) {
                processAndStoreValue(secrets, currentKey, currentValueLines.join("\n"))
            }

            // Start the new key/value
            currentKey = match[1].trim()
            currentValueLines = [match[2]] // Start new value array
            // console.log(`Started key: ${currentKey}, first value line: ${match[2]}`); // Debugging
        } else if (currentKey && line.trim() !== "") {
            // If we have a currentKey and the line is not empty,
            // and it didn't match the 'new key' regex, treat it as a continuation.
            // This includes lines with leading whitespace AND the closing brace '}' which might not have it.
            currentValueLines.push(line)
        } else if (line.trim() && !line.includes("No secrets configured for this project")) {
            // Log unexpected lines that aren't continuations or the "No secrets" message
            console.warn(`Skipping unexpected/malformed line: ${line}`)
        }
    }

    // Process the last key/value pair after the loop
    if (currentKey) {
        processAndStoreValue(secrets, currentKey, currentValueLines.join("\n"))
    }

    return secrets
}

try {
    const { execSync } = require("child_process")
    const fs = require("fs")
    const path = require("path")

    const projectPath = "dotnet_server/ChattyMcChatface.Api"
    const outputFilePath = path.join(__dirname, "appsettings.Docker.json")

    const command = `dotnet user-secrets list --project ${projectPath}`
    console.log(`Executing: ${command}`)
    const output = execSync(command, { encoding: "utf8", cwd: __dirname })
    console.log("Raw secrets output received.")

    // Use the new parsing function
    const secrets = parseSecretsOutput(output)

    const jsonContent = JSON.stringify(secrets, null, 2) // Pretty print
    console.log(`\nWriting generated configuration structure to ${outputFilePath}`)
    fs.writeFileSync(outputFilePath, jsonContent)

    console.log(`\nSuccessfully generated ${outputFilePath}`)
} catch (error) {
    console.error(`Error generating secrets file: ${error.message}`)
    if (error.stderr) {
        console.error(`stderr: ${error.stderr}`)
    }
    process.exit(1)
}

module.exports = { parseSecretsOutput }
