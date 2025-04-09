using ChattyMcChatface.Core.Dtos;
using System.Threading.Tasks;

namespace ChattyMcChatface.Core.Services;
public interface IPersonaService
{
    Task GenerateResponseAsync(int chatroomId, ChatMessageDto triggeringMessage);
}