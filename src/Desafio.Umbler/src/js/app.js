const Request = window.Request
const Headers = window.Headers
const fetch = window.fetch

class Api {
    async request(method, url, body) {
        if (body) body = JSON.stringify(body)

        const request = new Request('/api/' + url, {
            method: method,
            body: body,
            credentials: 'same-origin',
            headers: new Headers({
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            })
        })

        const resp = await fetch(request)

        // Tenta ler como JSON; se não der, lê como texto
        let data = null
        const contentType = resp.headers.get('content-type') || ''
        try {
            if (contentType.includes('application/json')) {
                data = await resp.json()
            } else {
                data = await resp.text()
            }
        } catch (e) {
            // se falhar, tenta como texto
            data = await resp.text().catch(() => null)
        }

        // Se não for OK, devolve um objeto padrão de erro
        if (!resp.ok) {
            return {
                requestStatus: resp.status,
                error: (typeof data === 'string' && data)
                    ? data
                    : (data && data.message ? data.message : (resp.statusText || 'Erro'))

            }
        }

        // Se OK e veio string, encapsula
        if (typeof data === 'string') {
            return { requestStatus: 200, data }
        }

        return data
    }

    async getDomain(domainOrIp) {
        return this.request('GET', `domain/${domainOrIp}`)
    }
}

const api = new Api()

//função para padronizar o Input do Usuário
function normalizeDomainInput(value)
{
    if (!value) return ''

    
    let formattedValue = value.trim().toLowerCase()

    // remove http/https
    formattedValue = formattedValue.replace(/^https?:\/\//, '')

    // remove caminho 'extra' (tudo depois da primeira /)
    formattedValue = formattedValue.split('/')[0]

    // remove espaços em branco extras
    formattedValue = formattedValue.trim()

    return formattedValue
}

function isValidDomainLike(value)
{
    
    if (!value) return false
    if (!value.includes('.')) return false
    if (value.startsWith('.') || value.endsWith('.')) return false
    return true
}

function formatResult(result) {
    var name = result.name || result.Name || '';
    var ip = result.ip || result.Ip || '(não encontrado)';
    var hostedAt = result.hostedAt || result.HostedAt || '(não informado)';
    var ttl = (result.ttl !== undefined ? result.ttl : result.Ttl);
    if (ttl === undefined || ttl === null) ttl = '(não informado)';
    var whois = result.whoIs || result.WhoIs || '';

    return (
        'DOMINIO: ' + name + '\n' +
        'IP: ' + ip + '\n' +
        'HOSPEDADO EM: ' + hostedAt + '\n' +
        'TTL: ' + ttl + '\n\n' +
        'WHOIS:\n' + whois
    );
}


var callback = () => {
    const btn = document.getElementById('btn-search')
    const txt = document.getElementById('txt-search')
    const result = document.getElementById('whois-results')

    if (btn)
    {
        btn.onclick = async () => {
            const input = normalizeDomainInput(txt.value)

            // Valida Frontend 
            if (!isValidDomainLike(input))
            {
                result.innerHTML = 'Dominio invalido. Ex: google.com'
                return
            }

            result.innerHTML = 'Consultando...'

            try
            {
                const response = await api.getDomain(input)

                if (response && response.requestStatus && response.requestStatus !== 200)
                {
                    result.innerHTML = `Erro (${response.requestStatus}): ${response.error || 'Domínio inválido.'}`
                    return
                }

                result.textContent = formatResult(response);
            } catch (err)
            {
                result.innerHTML = `Erro: ${err.message || err}`
            }
        }
    }
}

if (document.readyState === 'complete' || (document.readyState !== 'loading' && !document.documentElement.doScroll))
{
    callback()
}
else
{
    document.addEventListener('DOMContentLoaded', callback)
}
