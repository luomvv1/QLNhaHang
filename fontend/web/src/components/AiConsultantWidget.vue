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
import { nextTick, onMounted, onUnmounted, ref } from 'vue';
import axios from 'axios';

const AI_API = 'http://localhost:5079/api/customer/ai/chat';

const isOpen = ref(false);
const inputMessage = ref('');
const inputRef = ref(null);
const isLoading = ref(false);
const conversationId = ref(null);
const conversationToken = ref(null);
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
    const response = await axios.post(AI_API, { conversationId: conversationId.value, conversationToken: conversationToken.value, message: text });
    if (response.data?.success) {
      const data = response.data.data;
      conversationId.value = data.conversationId;
      conversationToken.value = data.conversationToken || null;
      sessionStorage.setItem('ai_conversation_id', String(data.conversationId));
      sessionStorage.setItem('ai_conversation_token', data.conversationToken || '');
      sessionStorage.setItem('ai_conversation_actor', localStorage.getItem('userId') || 'guest');
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
  conversationToken.value = null;
  messages.value = [];
  sessionStorage.removeItem('ai_conversation_id');
  sessionStorage.removeItem('ai_conversation_token');
  sessionStorage.removeItem('ai_conversation_actor');
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
  window.addEventListener('auth-changed', clearChat);
  const savedConversationId = sessionStorage.getItem('ai_conversation_id');
  if (savedConversationId && sessionStorage.getItem('ai_conversation_actor') === (localStorage.getItem('userId') || 'guest')) {
    conversationId.value = Number(savedConversationId);
    conversationToken.value = sessionStorage.getItem('ai_conversation_token') || null;
  }
});
onUnmounted(() => window.removeEventListener('auth-changed', clearChat));
</script>

<style scoped>
.ai-widget-wrapper { position: fixed; right: 22px; bottom: 22px; z-index: 1060; font-family: inherit; }
.ai-launcher { display: inline-flex; align-items: center; gap: 10px; min-height: 54px; padding: 8px 18px 8px 12px; color: #fff; background: linear-gradient(135deg, #7A1C28 0%, #5E141D 100%); border: 1.5px solid rgba(184, 125, 50, 0.6); border-radius: 9999px; box-shadow: 0 12px 32px rgba(122, 28, 40, 0.45); cursor: pointer; transition: transform .25s cubic-bezier(0.16, 1, 0.3, 1), box-shadow .25s ease; }
.ai-launcher:hover { transform: translateY(-3px) scale(1.02); box-shadow: 0 16px 38px rgba(122, 28, 40, 0.6); }
.launcher-icon { display: grid; place-items: center; width: 36px; height: 36px; color: #7A1C28; background: #FCE7B0; border-radius: 50%; font-size: 18px; font-weight: 900; }
.launcher-copy { display: grid; gap: 1px; text-align: left; }.launcher-copy strong { font-size: 13px; letter-spacing: 0.02em; }.launcher-copy small { color: rgba(255,255,255,.8); font-size: 11px; }.launcher-status { width: 8px; height: 8px; margin-left: 5px; background: #F8D38D; border: 2px solid rgba(255,255,255,.65); border-radius: 50%; box-shadow: 0 0 6px rgba(248, 211, 141, 0.8); }
.ai-chat-shell { display: flex; flex-direction: column; width: 410px; height: min(650px, calc(100vh - 44px)); max-width: calc(100vw - 28px); overflow: hidden; background: #fff; border: 1.5px solid rgba(184, 125, 50, 0.3); border-radius: 16px; box-shadow: 0 24px 60px rgba(44, 30, 20, .28); animation: chat-enter .25s cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes chat-enter { from { opacity: 0; transform: translateY(16px); } to { opacity: 1; transform: translateY(0); } }
.ai-chat-header { display: flex; align-items: center; justify-content: space-between; gap: 14px; padding: 16px 18px 14px; color: #fff; background: linear-gradient(135deg, #7A1C28 0%, #5E141D 100%); }.assistant-identity { display: flex; align-items: center; gap: 10px; }.assistant-avatar { display: grid; place-items: center; width: 38px; height: 38px; color: #7A1C28; background: #FCE7B0; border-radius: 10px; font-size: 13px; font-weight: 900; letter-spacing: .06em; }.assistant-identity h2 { margin: 0; font-size: 15px; font-weight: 800; letter-spacing: 0; }.assistant-status { display: flex; align-items: center; gap: 5px; margin-top: 3px; color: rgba(255,255,255,.75); font-size: 11px; }.assistant-status span { width: 6px; height: 6px; background: #F8D38D; border-radius: 50%; }.header-actions { display: flex; gap: 4px; }.header-action { display: grid; place-items: center; width: 30px; height: 30px; color: rgba(255,255,255,.85); background: transparent; border: 1px solid rgba(255,255,255,.25); border-radius: 6px; cursor: pointer; font-size: 18px; line-height: 1; }.header-action:hover { color: #fff; background: rgba(255,255,255,.15); }
.ai-intro-bar { display: flex; gap: 9px; align-items: center; padding: 11px 15px; background: #FAF7F2; border-bottom: 1px solid #EADFD0; }.intro-mark { display: grid; place-items: center; width: 23px; height: 23px; color: #7A1C28; background: #FCE7B0; border-radius: 5px; font-size: 12px; }.ai-intro-bar strong, .ai-intro-bar small { display: block; }.ai-intro-bar strong { color: #1F1A17; font-size: 12px; }.ai-intro-bar small { margin-top: 2px; color: #6E6157; font-size: 11px; }
.quick-prompts { display: flex; gap: 6px; padding: 0 0 9px; overflow-x: auto; background: #fff; scrollbar-width: thin; }.prompt-chip { flex: 0 0 auto; padding: 6px 12px; color: #7A1C28; background: #FAF7F2; border: 1px solid rgba(184, 125, 50, 0.35); border-radius: 9999px; cursor: pointer; font: inherit; font-size: 12px; font-weight: 600; white-space: nowrap; transition: all 0.2s ease; }.prompt-chip:hover { color: #fff; background: #7A1C28; border-color: #7A1C28; }.prompt-chip:disabled { opacity: .55; cursor: not-allowed; }
.ai-messages-container { flex: 1; overflow-y: auto; padding: 16px 14px 18px; background: #FAF7F2; scrollbar-color: #cbd5e1 transparent; scrollbar-width: thin; }.message-row { display: flex; gap: 8px; margin-bottom: 16px; }.message-row-bot { align-items: flex-start; }.message-row-user { justify-content: flex-end; }.message-avatar { display: grid; flex: 0 0 28px; place-items: center; width: 28px; height: 28px; color: #7A1C28; background: #F4ECE1; border: 1px solid rgba(184, 125, 50, 0.3); border-radius: 8px; font-size: 10px; font-weight: 900; }.message-stack { display: flex; flex-direction: column; gap: 4px; max-width: 86%; }.message-author { color: #8C7F75; font-size: 11px; font-weight: 700; }.message-bubble { padding: 11px 14px; border-radius: 12px; font-size: 13px; line-height: 1.55; overflow-wrap: anywhere; }.message-bubble p { margin: 0 0 7px; }.message-bubble p:last-child { margin-bottom: 0; }.message-bubble-user { max-width: 82%; color: #fff; background: linear-gradient(135deg, #7A1C28 0%, #63141F 100%); border-radius: 12px 12px 2px 12px; box-shadow: 0 4px 12px rgba(122, 28, 40, 0.25); }.message-bubble-bot { color: #1F1A17; background: #fff; border: 1px solid #EADFD0; border-radius: 2px 12px 12px 12px; box-shadow: 0 2px 8px rgba(44,30,20,.04); }.message-muted { color: #6E6157; font-size: 12px; }.message-text :deep(strong) { color: #7A1C28; }.typing-bubble { display: flex; align-items: center; gap: 4px; padding: 13px 14px; }.typing-bubble span { width: 6px; height: 6px; background: #B87D32; border-radius: 50%; animation: typing 1.1s infinite ease-in-out; }.typing-bubble span:nth-child(2) { animation-delay: .15s; }.typing-bubble span:nth-child(3) { animation-delay: .3s; }@keyframes typing { 0%, 70%, 100% { opacity: .35; transform: translateY(0); } 35% { opacity: 1; transform: translateY(-3px); } }
.recommendations { margin-top: 11px; padding-top: 10px; border-top: 1px solid #EADFD0; }.recommendations-heading { display: flex; justify-content: space-between; margin-bottom: 7px; color: #7A1C28; font-size: 11px; font-weight: 800; }.recommendations-heading small { color: #8C7F75; font-weight: 600; }.food-recommendation { display: flex; align-items: center; gap: 8px; padding: 8px 10px; margin-top: 6px; background: #FFFFFF; border: 1px solid #EADFD0; border-radius: 8px; }.food-recommendation-icon { display: grid; flex: 0 0 28px; place-items: center; width: 28px; height: 28px; color: #7A1C28; background: #FCE7B0; border-radius: 6px; font-size: 11px; font-weight: 900; }.food-recommendation-info { min-width: 0; flex: 1; }.food-recommendation-info strong, .food-recommendation-info span { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.food-recommendation-info strong { color: #1F1A17; font-size: 12px; }.food-recommendation-info span { margin-top: 2px; color: #6E6157; font-size: 10px; }.food-recommendation-link { flex: 0 0 auto; padding: 5px 10px; color: #7A1C28; border: 1.5px solid #7A1C28; border-radius: 9999px; font-size: 11px; font-weight: 700; text-decoration: none; transition: all 0.2s ease; }.food-recommendation-link:hover { color: #fff; background: #7A1C28; }
.ai-chat-footer { padding: 12px 14px 10px; background: #fff; border-top: 1px solid #EADFD0; }.message-composer { display: flex; gap: 8px; align-items: center; padding: 6px 6px 6px 14px; background: #FAF7F2; border: 1.5px solid #EADFD0; border-radius: 9999px; transition: all 0.2s ease; }.message-composer:focus-within { border-color: #B87D32; box-shadow: 0 0 0 3px rgba(184, 125, 50, .15); background: #fff; }.message-composer input { min-width: 0; flex: 1; padding: 5px 0; color: #1F1A17; background: transparent; border: 0; outline: 0; font: inherit; font-size: 13px; }.message-composer input::placeholder { color: #9C8E84; }.send-button { display: grid; flex: 0 0 34px; place-items: center; width: 34px; height: 34px; color: #fff; background: #7A1C28; border: 0; border-radius: 50%; cursor: pointer; font-size: 18px; font-weight: 700; line-height: 1; transition: all 0.2s ease; }.send-button:hover:not(:disabled) { background: #942433; transform: scale(1.05); }.send-button:disabled { background: #D6CCC2; cursor: not-allowed; }.composer-note { margin-top: 6px; color: #8C7F75; font-size: 10px; text-align: center; }
@media (max-width: 520px) { .ai-widget-wrapper { right: 12px; bottom: 12px; left: 12px; }.ai-launcher { width: 100%; justify-content: center; }.ai-chat-shell { width: 100%; max-width: none; height: calc(100dvh - 24px); max-height: none; border-radius: 12px; }.ai-chat-header { padding: 14px; }.food-recommendation-link { padding-left: 8px; padding-right: 8px; }.message-stack { max-width: 88%; } }
</style>
