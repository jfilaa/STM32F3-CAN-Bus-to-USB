/**
  ******************************************************************************
  * @file    main.c
  * @author  MCD Application Team
  * @version V4.0.0
  * @date    21-January-2013
  * @brief   Virtual Com Port Demo main file
  ******************************************************************************
  * @attention
  *
  * <h2><center>&copy; COPYRIGHT 2013 STMicroelectronics</center></h2>
  *
  * Licensed under MCD-ST Liberty SW License Agreement V2, (the "License");
  * You may not use this file except in compliance with the License.
  * You may obtain a copy of the License at:
  *
  *        http://www.st.com/software_license_agreement_liberty_v2
  *
  * Unless required by applicable law or agreed to in writing, software 
  * distributed under the License is distributed on an "AS IS" BASIS, 
  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  * See the License for the specific language governing permissions and
  * limitations under the License.
  *
  ******************************************************************************
  */


/* Includes ------------------------------------------------------------------*/
#include "hw_config.h"
#include "usb_lib.h"
#include "usb_desc.h"
#include "usb_pwr.h"
#include <stdio.h>
#include <string.h>

/* Private typedef -----------------------------------------------------------*/

#define USER_BUTTON_PIN                GPIO_Pin_0
#define USER_BUTTON_GPIO_PORT          GPIOA
#define USER_BUTTON_GPIO_CLK           RCC_AHBPeriph_GPIOA

/* Private define ------------------------------------------------------------*/
/* Private macro -------------------------------------------------------------*/
/* Private variables ---------------------------------------------------------*/
char TextBuffer[500];
/* Extern variables ----------------------------------------------------------*/
/* Private function prototypes -----------------------------------------------*/
/* Private functions ---------------------------------------------------------*/

/**
  * @brief  Cekani
  * @param nTick: delka cekaci smycky
  * @retval : None
  */
void Delay(__IO uint32_t nTick)
{
  for(; nTick != 0; nTick--);
}

void SendCanMsg(CanRxMsg RxMessage)
{
  int i;
    char* buf2 = &TextBuffer[0] + sprintf(&TextBuffer[0], "0x%04X ", RxMessage.StdId);
    //buf2 += sprintf(buf2, "0x%04X ", RxMessage.ExtId);
    char* endofbuf = &TextBuffer[0] + sizeof(TextBuffer);
    for (i = 0; i < RxMessage.DLC; i++)
    {
        // i use 5 here since we are going to add at most 
        //  3 chars, need a space for the end '\n' and need
        //   a null terminator
        if (buf2 + 5 < endofbuf)
        {
            if (i > 0)
            {
                buf2 += sprintf(buf2, " ");
            }
            buf2 += sprintf(buf2, "%02X", RxMessage.Data[i]);
        }
    }
    buf2 += sprintf(buf2, "\r\n");
    UserToPMABufferCopy((uint8_t*)&TextBuffer[0], ENDP1_TXADDR, strlen(TextBuffer));
    SetEPTxCount(ENDP1, strlen(TextBuffer));
    SetEPTxValid(ENDP1);
}

/*******************************************************************************
* Function Name  : main.
* Description    : Main routine.
* Input          : None.
* Output         : None.
* Return         : None.
*******************************************************************************/
int main(void)
{
  Set_System();
#if 1
  Set_USBClock();
  USB_Interrupts_Config();
  USB_Init();
#endif
  
  Set_CAN();
  
  /* Initialize LEDs and User Button available on STM32F3-Discovery board */
  STM_EVAL_LEDInit(LED3);
  STM_EVAL_LEDInit(LED4);
  STM_EVAL_LEDInit(LED5);
  STM_EVAL_LEDInit(LED6);
  STM_EVAL_LEDInit(LED7);
  STM_EVAL_LEDInit(LED8);
  STM_EVAL_LEDInit(LED9);
  STM_EVAL_LEDInit(LED10);
  
  STM_EVAL_PBInit(BUTTON_USER, BUTTON_MODE_GPIO);
  
  /* LEDs Off */
  STM_EVAL_LEDOff(LED3);
  STM_EVAL_LEDOff(LED6);
  STM_EVAL_LEDOff(LED7);
  STM_EVAL_LEDOff(LED4);
  STM_EVAL_LEDOff(LED10);
  STM_EVAL_LEDOff(LED8);
  STM_EVAL_LEDOff(LED9);
  STM_EVAL_LEDOff(LED5);
  
  STM_EVAL_LEDOn(LED3);
  while (1)
  {
    while(GPIO_ReadInputDataBit(USER_BUTTON_GPIO_PORT, USER_BUTTON_PIN) != Bit_RESET){}
    Delay(0xFFFFF);
    //GPIO_WriteBit(GPIOE, GPIO_Pin_1, 0);      // zapneme cervenou    LEDku
    STM_EVAL_LEDOff(LED10);
    while(GPIO_ReadInputDataBit(USER_BUTTON_GPIO_PORT, USER_BUTTON_PIN) != Bit_SET){}
    
#define _SEND_CAN_MSG_USB_    
#ifdef _SEND_CAN_MSG_USB_
    
    CanRxMsg RxMessage;
    RxMessage.Data[0] = 1;
    RxMessage.Data[1] = 2;
    RxMessage.Data[2] = 3;
    RxMessage.Data[3] = 4;
    RxMessage.DLC = 4;
    RxMessage.StdId = 0x1234;
    RxMessage.ExtId = 0x1;
    
    SendCanMsg(RxMessage);
    
#else
    
    CanTxMsg TxMessage;
    
    /* Transmit Structure preparation */
    TxMessage.StdId = 0x359;
    TxMessage.ExtId = 0x00;
    TxMessage.RTR = CAN_RTR_DATA;
    TxMessage.IDE = CAN_ID_STD;
    TxMessage.DLC = 8; // délka dat

    TxMessage.Data[0] = 0x1F;
    TxMessage.Data[1] = 0x01;
    TxMessage.Data[2] = 0x00;
    TxMessage.Data[3] = 0x00;
    TxMessage.Data[4] = 0x00;
    TxMessage.Data[5] = 0x00;
    TxMessage.Data[6] = 0x00;
    TxMessage.Data[7] = 0x00;
    CAN_Transmit(CAN1, &TxMessage);
#endif
    
    STM_EVAL_LEDOn(LED10);
  }
}
#ifdef USE_FULL_ASSERT
/*******************************************************************************
* Function Name  : assert_failed
* Description    : Reports the name of the source file and the source line number
*                  where the assert_param error has occurred.
* Input          : - file: pointer to the source file name
*                  - line: assert_param error line source number
* Output         : None
* Return         : None
*******************************************************************************/
void assert_failed(uint8_t* file, uint32_t line)
{
  /* User can add his own implementation to report the file name and line number,
     ex: printf("Wrong parameters value: file %s on line %d\r\n", file, line) */

  /* Infinite loop */
  while (1)
  {}
}
#endif

/************************ (C) COPYRIGHT STMicroelectronics *****END OF FILE****/
