import { Pipe, PipeTransform } from '@angular/core';
import { ChatMessageDto, MessageRole } from '../services/chat.service'; // Adjust path if needed

@Pipe({
  name: 'parseAiMessage',
  standalone: true,
})
export class ParseAiMessagePipe implements PipeTransform {
  transform(value: ChatMessageDto): string {
    if (!value) {
      return ''; // Handle null/undefined input gracefully
    }

    if (value.role === MessageRole.Assistant && value.text) {
      try {
        const parsedResponse = JSON.parse(value.text);
        if (
          parsedResponse &&
          typeof parsedResponse.message === 'string' &&
          parsedResponse.message.trim() !== ''
        ) {
          return parsedResponse.message; // Return the parsed message
        } else {
          console.error(
            'ParseAiMessagePipe: Parsed AI response is missing or has empty "message" property:',
            parsedResponse,
            'Original text:',
            value.text
          );
          return value.text; // Return original text if structure is wrong
        }
      } catch (error) {
        // If it's not valid JSON, it might be a regular assistant message or an error occurred during generation.
        // Log the error for debugging but return the original text.
        // console.warn('ParseAiMessagePipe: Failed to parse AI response JSON, returning original text:', error, 'Raw text:', value.text);
        return value.text; // Return original text if parsing fails
      }
    }

    // Return original text for non-assistant messages or if text is empty
    return value.text;
  }
}
