// MyCaffe.test.extension.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "FunctionIDs.h"

// Used to call back to the parent CudaDnnDLL
LPFNINTERNAL_INVOKEFLOAT m_pfnf = NULL;
LPFNINTERNAL_INVOKEDOUBLE m_pfndf = NULL;
LPFNINTERNAL_ALLOCHOSTFLOAT m_pfnfAllocHost = NULL;
LPFNINTERNAL_ALLOCHOSTDOUBLE m_pfndfAllocHost = NULL;
LONG m_lKernel;

float m_rgDataF[1024];
double m_rgDataD[1024];


extern "C" LONG WINAPI DLL_InitFloatCustomExtension(HMODULE hParent, long lKernelIdx)
{
	m_pfnf = (LPFNINTERNAL_INVOKEFLOAT)GetProcAddress(hParent, SZFN_INTERNAL_INVOKEFLOAT);
	if (m_pfnf == NULL)
		return ERROR_INVALID_PARAMETER;

	m_pfnfAllocHost = (LPFNINTERNAL_ALLOCHOSTFLOAT)GetProcAddress(hParent, SZFN_INTERNAL_ALLOCHOSTFLT);
	if (m_pfnfAllocHost == NULL)
		return ERROR_INVALID_PARAMETER;

	m_lKernel = lKernelIdx;

	return 0;
}

extern "C" LONG WINAPI DLL_InvokeFloatCustomExtension(LONG lfnIdx, float* pInput, LONG lInput, float** ppOutput, LONG* plOutput, LPTSTR szErr, LONG lErrMax)
{
	LONG lErr;

	if (m_pfnf == NULL)
		return PEER_E_NOT_INITIALIZED;

	switch (lfnIdx)
	{
		case 1:
			for (int i = 0; i < lInput && i < 1024; i++)
			{
				m_rgDataF[i] = pInput[i] * pInput[i];
			}
			break;

		case 2:
			for (int i = 0; i < lInput && i < 1024; i++)
			{
				m_rgDataF[i] = pInput[i] * pInput[i] * pInput[i];
			}
			break;

		default:
			_tcsncpy(szErr, _T("The function specified is not supported."), lErrMax);
			return ERROR_NOT_SUPPORTED;
	}

	if (lErr = (*m_pfnfAllocHost)(m_lKernel, lInput, ppOutput, m_rgDataF, false))
		return lErr;

	*plOutput = lInput;

	return 0;
}

extern "C" LONG WINAPI DLL_InitDoubleCustomExtension(HMODULE hParent, long lKernelIdx)
{
	m_pfndf = (LPFNINTERNAL_INVOKEDOUBLE)GetProcAddress(hParent, SZFN_INTERNAL_INVOKEDOUBLE);
	if (m_pfndf == NULL)
		return ERROR_INVALID_PARAMETER;

	m_pfndfAllocHost = (LPFNINTERNAL_ALLOCHOSTDOUBLE)GetProcAddress(hParent, SZFN_INTERNAL_ALLOCHOSTDBL);
	if (m_pfndfAllocHost == NULL)
		return ERROR_INVALID_PARAMETER;

	m_lKernel = lKernelIdx;

	return 0;
}

extern "C" LONG WINAPI DLL_InvokeDoubleCustomExtension(LONG lfnIdx, double* pInput, LONG lInput, double** ppOutput, LONG* plOutput, LPTSTR szErr, LONG lErrMax)
{
	LONG lErr;

	if (m_pfndf == NULL)
		return PEER_E_NOT_INITIALIZED;

	switch (lfnIdx)
	{
	case 1:
		for (int i = 0; i < lInput && i < 1024; i++)
		{
			m_rgDataD[i] = pInput[i] * pInput[i];
		}
		break;

	case 2:
		for (int i = 0; i < lInput && i < 1024; i++)
		{
			m_rgDataD[i] = pInput[i] * pInput[i] * pInput[i];
		}
		break;

	default:
		_tcsncpy(szErr, _T("The function specified is not supported."), lErrMax);
		return ERROR_NOT_SUPPORTED;
	}

	if (lErr = (*m_pfndfAllocHost)(m_lKernel, lInput, ppOutput, m_rgDataD, false))
		return lErr;

	*plOutput = lInput;

	return 0;
}