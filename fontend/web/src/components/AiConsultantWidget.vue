<template>
  <div class="ai-widget-wrapper">
    <button
      v-if="!isOpen"
      class="ai-launcher"
      type="button"
      aria-label="Mở trợ lý ẩm thực AI"
      aria-expanded="false"
      @click="toggleChat"
    >
      <span class="launcher-icon">✦</span>
      <span class="launcher-copy"><strong>Trợ lý Hương Sen</strong><small>Tư vấn món ăn</small></span>
      <span class="launcher-status" aria-hidden="true"></span>
    </button>

    <section v-else class="ai-chat-shell" role="dialog" aria-modal="false" aria-label="Trợ lý ẩm thực Hương Sen">
      <header class="ai-chat-header">
        <div class="assistant-identity">
          <div class="assistant-avatar" aria-hidden="true">HS</div>
          <div>
            <h2>Trợ lý ẩm thực</h2>
            <div class="assistant-status"><span></span> Đang trực tuyến · Gemini AI</div>
          </div>
        </div>
        <div class="header-actions">
          <button type="button" class="header-action" title="Làm mới cuộc trò chuyện" aria-label="Làm mới cuộc trò chuyện" @click="clearChat">↻</button>
          <button type="button" class="header-action" title="Đóng trợ lý" aria-label="Đóng trợ lý" @click="isOpen = false">×</button>
        </div>
      </header>

      <div class="ai-intro-bar">
        <span class="intro-mark">✦</span>
        <div><strong>Tư vấn riêng cho bữa ăn của bạn</strong><small>Hỏi về món, khẩu vị, số người hoặc ngân sách.</small></div>
      </div>

      <div ref="messagesContainer" class="ai-messages-container" aria-live="polite">
        <div class="message-row message-row-bot">
          <div class="message-avatar">HS</div>
          <div class="message-stack">
            <span class="message-author">Trợ lý Hương Sen</span>
            <div class="message-bubble message-bubble-bot">
              <p>Chào bạn, tôi có thể giúp chọn món theo khẩu vị, số người và ngân sách.</p>
              <p class="message-muted">Bạn muốn dùng món lẩu, nướng, hải sản hay một bữa tiệc gia đình?</p>
            </div>
          </div>
        </div>

        <div v-for="(msg, index) in messages" :key="`${index}-${msg.sender}`" :class="['message-row', msg.sender === 'user' ? 'message-row-user' : 'message-row-bot']">
          <template v-if="msg.sender === 'user'">
            <div class="message-bubble message-bubble-user">{{ msg.text }}</div>
          </template>
          <template v-else>
            <div class="message-avatar">HS</div>
            <div class="message-stack">
              <span class="message-author">Trợ lý Hương Sen</span>
              <div class="message-bubble message-bubble-bot">
                <div class="message-text" v-html="formatMessage(msg.text)"></div>

                <div v-if="msg.recommendedFoods?.length" class="recommendations">
                  <div class="recommendations-heading"><span>Gợi ý phù hợp</span><small>{{ msg.recommendedFoods.length }} món</small></div>
                  <article v-for="food in msg.recommendedFoods" :key="food.foodId" class="food-recommendation">
                    <div class="food-recommendation-icon">M</div>
                    <div class="food-recommendation-info">
                      <strong>{{ food.foodName }}</strong>
                      <span>{{ food.categoryName }} · từ {{ formatCurrency(food.minPrice) }}</span>
                    </div>
                    <router-link to="/customer/reservation" class="food-recommendation-link">Đặt bàn</router-link>
                  </article>
                </div>
              </div>
            </div>
          </template>
        </div>

        <div v-if="isLoading" class="message-row message-row-bot">
          <div class="message-avatar">HS</div>
          <div class="message-stack"><span class="message-author">Trợ lý Hương Sen</span><div class="message-bubble message-bubble-bot typing-bubble"><span></span><span></span><span></span></div></div>
        </div>
      </div>

      <footer class="ai-chat-footer">
        <div class="quick-prompts" aria-label="Gợi ý câu hỏi">
          <button v-for="chip in promptChips" :key="chip" type="button" class="prompt-chip" :disabled="isLoading" @click="sendMessage(chip)">
            {{ chip }}
          </button>
        </div>
        <form class="message-composer" @submit.prevent="handleSend">
          <input ref="inputRef" v-model="inputMessage" type="text" :disabled="isLoading" placeholder="Ví dụ: Gợi ý lẩu cho 4 người..." aria-label="Nhập câu hỏi cho trợ lý AI" />
          <button type="submit" class="send-button" aria-label="Gửi tin nhắn" :disabled="!inputMessage.trim() || isLoading">→</button>
        </form>
        <div class="composer-note">Trợ lý AI có thể cần xác nhận lại thông tin đặt bàn.</div>
      </footer>
    </section>
  </div>
</template>

<script setup>
import { nextTick, onMounted, ref } from 'vue';
import axios from 'axios';

const AI_API = 'http://localhost:5079/api/customer/ai/chat';

const isOpen = ref(false);
const inputMessage = ref('');
const inputRef = ref(null);
const isLoading = ref(false);
const conversationId = ref(null);
const messages = ref([]);
const messagesContainer = ref(null);

const promptChips = [
  'Gợi ý món lẩu ngon',
  'Combo tiệc 4 người',
  'Món thanh đạm',
  'Hải sản tươi sống',
  'Món nướng đậm vị'
];

function toggleChat() {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    nextTick(() => inputRef.value?.focus());
    scrollToBottom();
  }
}

async function handleSend() {
  const text = inputMessage.value.trim();
  if (!text || isLoading.value) return;
  inputMessage.value = '';
  await sendMessage(text);
}

async function sendMessage(text) {
  messages.value.push({ sender: 'user', text });
  isLoading.value = true;
  scrollToBottom();

  try {
    const response = await axios.post(AI_API, { conversationId: conversationId.value, message: text });
    if (response.data?.success) {
      const data = response.data.data;
      conversationId.value = data.conversationId;
      sessionStorage.setItem('ai_conversation_id', String(data.conversationId));
      messages.value.push({ sender: 'ai', text: data.reply, recommendedFoods: data.recommendedFoods || [] });
    } else {
      messages.value.push({ sender: 'ai', text: 'Tôi chưa nhận được câu trả lời phù hợp. Bạn thử hỏi lại theo cách khác nhé.', recommendedFoods: [] });
    }
  } catch (error) {
    messages.value.push({ sender: 'ai', text: 'Đường truyền đang bận một chút. Bạn vui lòng thử lại sau giây lát nhé.', recommendedFoods: [] });
  } finally {
    isLoading.value = false;
    scrollToBottom();
  }
}

function clearChat() {
  conversationId.value = null;
  messages.value = [];
  sessionStorage.removeItem('ai_conversation_id');
  nextTick(() => inputRef.value?.focus());
}

function scrollToBottom() {
  nextTick(() => {
    if (messagesContainer.value) messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  });
}

function formatMessage(text) {
  if (!text) return '';
  const escaped = String(text).replace(/[&<>"']/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' }[char]));
  return escaped.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>').replace(/\n/g, '<br>');
}

function formatCurrency(value) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value || 0);
}

onMounted(() => {
  const savedConversationId = sessionStorage.getItem('ai_conversation_id');
  if (savedConversationId) conversationId.value = Number(savedConversationId);
});
</script>

<style scoped>
.ai-widget-wrapper { position: fixed; right: 22px; bottom: 22px; z-index: 1060; font-family: inherit; }
.ai-launcher { display: inline-flex; align-items: center; gap: 10px; min-height: 54px; padding: 8px 16px 8px 10px; color: #fff; background: #155e55; border: 1px solid #d0a35b; border-radius: 28px; box-shadow: 0 12px 30px rgba(15, 59, 51, .25); cursor: pointer; transition: transform .18s ease, box-shadow .18s ease; }
.ai-launcher:hover { transform: translateY(-2px); box-shadow: 0 16px 34px rgba(15, 59, 51, .3); }
.launcher-icon { display: grid; place-items: center; width: 36px; height: 36px; color: #173f37; background: #f4d9a6; border-radius: 50%; font-size: 20px; }
.launcher-copy { display: grid; gap: 1px; text-align: left; }.launcher-copy strong { font-size: 13px; }.launcher-copy small { color: rgba(255,255,255,.72); font-size: 11px; }.launcher-status { width: 8px; height: 8px; margin-left: 5px; background: #86efac; border: 2px solid rgba(255,255,255,.55); border-radius: 50%; }
.ai-chat-shell { display: flex; flex-direction: column; width: 410px; height: min(650px, calc(100vh - 44px)); max-width: calc(100vw - 28px); overflow: hidden; background: #fff; border: 1px solid #d9dee5; border-radius: 12px; box-shadow: 0 24px 60px rgba(15, 23, 42, .24); animation: chat-enter .2s ease-out; }
@keyframes chat-enter { from { opacity: 0; transform: translateY(12px); } to { opacity: 1; transform: translateY(0); } }
.ai-chat-header { display: flex; align-items: center; justify-content: space-between; gap: 14px; padding: 16px 16px 14px; color: #fff; background: #163f37; }.assistant-identity { display: flex; align-items: center; gap: 10px; }.assistant-avatar { display: grid; place-items: center; width: 38px; height: 38px; color: #163f37; background: #f4d9a6; border-radius: 9px; font-size: 12px; font-weight: 900; letter-spacing: .06em; }.assistant-identity h2 { margin: 0; font-size: 15px; font-weight: 800; letter-spacing: 0; }.assistant-status { display: flex; align-items: center; gap: 5px; margin-top: 3px; color: rgba(255,255,255,.68); font-size: 11px; }.assistant-status span { width: 6px; height: 6px; background: #86efac; border-radius: 50%; }.header-actions { display: flex; gap: 4px; }.header-action { display: grid; place-items: center; width: 30px; height: 30px; color: rgba(255,255,255,.82); background: transparent; border: 1px solid rgba(255,255,255,.2); border-radius: 5px; cursor: pointer; font-size: 18px; line-height: 1; }.header-action:hover { color: #fff; background: rgba(255,255,255,.1); }
.ai-intro-bar { display: flex; gap: 9px; align-items: center; padding: 11px 15px; background: #fffaf0; border-bottom: 1px solid #eee3cd; }.intro-mark { display: grid; place-items: center; width: 23px; height: 23px; color: #8a5b17; background: #f7e4bd; border-radius: 5px; }.ai-intro-bar strong, .ai-intro-bar small { display: block; }.ai-intro-bar strong { color: #334155; font-size: 12px; }.ai-intro-bar small { margin-top: 2px; color: #64748b; font-size: 11px; }
.quick-prompts { display: flex; gap: 6px; padding: 0 0 9px; overflow-x: auto; background: #fff; scrollbar-width: thin; }.prompt-chip { flex: 0 0 auto; padding: 6px 9px; color: #155e55; background: #f0fdfa; border: 1px solid #b7e4dc; border-radius: 5px; cursor: pointer; font: inherit; font-size: 11px; font-weight: 700; white-space: nowrap; }.prompt-chip:hover { color: #fff; background: #155e55; }.prompt-chip:disabled { opacity: .55; cursor: not-allowed; }
.ai-messages-container { flex: 1; overflow-y: auto; padding: 16px 14px 18px; background: #f8fafb; scrollbar-color: #cbd5e1 transparent; scrollbar-width: thin; }.message-row { display: flex; gap: 8px; margin-bottom: 16px; }.message-row-bot { align-items: flex-start; }.message-row-user { justify-content: flex-end; }.message-avatar { display: grid; flex: 0 0 27px; place-items: center; width: 27px; height: 27px; color: #155e55; background: #d9f1eb; border-radius: 6px; font-size: 9px; font-weight: 900; }.message-stack { display: flex; flex-direction: column; gap: 4px; max-width: 86%; }.message-author { color: #64748b; font-size: 10px; font-weight: 700; }.message-bubble { padding: 10px 12px; border-radius: 8px; font-size: 13px; line-height: 1.5; overflow-wrap: anywhere; }.message-bubble p { margin: 0 0 7px; }.message-bubble p:last-child { margin-bottom: 0; }.message-bubble-user { max-width: 82%; color: #fff; background: #155e55; border-radius: 8px 8px 2px 8px; }.message-bubble-bot { color: #263238; background: #fff; border: 1px solid #e2e8f0; border-radius: 2px 8px 8px 8px; box-shadow: 0 2px 5px rgba(15,23,42,.03); }.message-muted { color: #64748b; font-size: 12px; }.message-text :deep(strong) { color: #155e55; }.typing-bubble { display: flex; align-items: center; gap: 4px; padding: 13px 14px; }.typing-bubble span { width: 6px; height: 6px; background: #94a3b8; border-radius: 50%; animation: typing 1.1s infinite ease-in-out; }.typing-bubble span:nth-child(2) { animation-delay: .15s; }.typing-bubble span:nth-child(3) { animation-delay: .3s; }@keyframes typing { 0%, 70%, 100% { opacity: .35; transform: translateY(0); } 35% { opacity: 1; transform: translateY(-3px); } }
.recommendations { margin-top: 11px; padding-top: 10px; border-top: 1px solid #e5e7eb; }.recommendations-heading { display: flex; justify-content: space-between; margin-bottom: 7px; color: #155e55; font-size: 11px; font-weight: 800; }.recommendations-heading small { color: #94a3b8; font-weight: 600; }.food-recommendation { display: flex; align-items: center; gap: 8px; padding: 8px; margin-top: 6px; background: #fcfdfd; border: 1px solid #e2e8f0; border-radius: 6px; }.food-recommendation-icon { display: grid; flex: 0 0 28px; place-items: center; width: 28px; height: 28px; color: #8a5b17; background: #f7e4bd; border-radius: 5px; font-size: 10px; font-weight: 900; }.food-recommendation-info { min-width: 0; flex: 1; }.food-recommendation-info strong, .food-recommendation-info span { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.food-recommendation-info strong { color: #1f2937; font-size: 12px; }.food-recommendation-info span { margin-top: 2px; color: #64748b; font-size: 10px; }.food-recommendation-link { flex: 0 0 auto; padding: 5px 7px; color: #155e55; border: 1px solid #8dd3c6; border-radius: 4px; font-size: 10px; font-weight: 800; text-decoration: none; }.food-recommendation-link:hover { color: #fff; background: #155e55; }
.ai-chat-footer { padding: 10px 12px 9px; background: #fff; border-top: 1px solid #e2e8f0; }.message-composer { display: flex; gap: 7px; align-items: center; padding: 5px 5px 5px 11px; background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 7px; }.message-composer:focus-within { border-color: #5aa99a; box-shadow: 0 0 0 2px rgba(90,169,154,.15); }.message-composer input { min-width: 0; flex: 1; padding: 5px 0; color: #1f2937; background: transparent; border: 0; outline: 0; font: inherit; font-size: 13px; }.message-composer input::placeholder { color: #94a3b8; }.send-button { display: grid; flex: 0 0 32px; place-items: center; width: 32px; height: 32px; color: #fff; background: #155e55; border: 0; border-radius: 5px; cursor: pointer; font-size: 19px; font-weight: 700; line-height: 1; }.send-button:disabled { background: #cbd5e1; cursor: not-allowed; }.composer-note { margin-top: 6px; color: #94a3b8; font-size: 10px; text-align: center; }
@media (max-width: 520px) { .ai-widget-wrapper { right: 12px; bottom: 12px; left: 12px; }.ai-launcher { width: 100%; justify-content: center; }.ai-chat-shell { width: 100%; max-width: none; height: calc(100dvh - 24px); max-height: none; border-radius: 10px; }.ai-chat-header { padding: 14px; }.food-recommendation-link { padding-left: 6px; padding-right: 6px; }.message-stack { max-width: 88%; } }
</style>
