type instanceRequest = string[];
type instanceUsersRequest = number[];

type instanceResponse = "hello";
type instanceUserResponse = "hello user";

type Paths = {
  "api/instance/{instanceId}": {
    url: "api/instance/{instanceId}";
    request: instanceRequest;
    params: {
      instanceId: string;
    };
    method: "PUT";
    response:
      | CustomResponse<instanceResponse, 200>
      | CustomResponse<undefined, 400>;
  };
  "api/instance/{instanceId}/users": {
    url: "api/instance/{instanceId}/users";
    request: instanceUsersRequest;
    params: {
      instanceId: string;
    };
    method: "GET";
    response: instanceUserResponse;
  };
};

export function getMyApiFetch(
  /**
   * custom fetch function to use
   */
  fetcher: typeof window.fetch = window.fetch
) {
  return <P extends keyof Paths>(url: P, method: Paths[P]["method"]) =>
    (
      params: Paths[P]["params"],
      request: Paths[P]["request"],
      init?: Omit<CustomRequestInit<Paths[P]["method"]>, "method">
    ) => {
      return fetcher(
        replaceUrl(url, params),
        Object.assign({}, init, { method, body: JSON.stringify(request) })
      ) as Promise<Paths[P]["response"]>;
    };
}

function replaceUrl(url: string, dictionary: Record<string, string>) {
  for (const key in dictionary) {
    url = url.replace(new RegExp(`\{${key}\}`, "g"), dictionary[key]);
  }
  return url;
}

interface CustomRequestInit<Method extends string>
  extends Omit<RequestInit, "body"> {
  method: Method;
}

interface CustomResponse<T, S extends number = 200> extends Response {
  status: S;
  json: () => Promise<T>;
}

class SomeProvider {
  private MyApiFetch = getMyApiFetch(window.fetch);

  public validateConnection = this.MyApiFetch(
    "api/instance/{instanceId}",
    "PUT"
  );
}

export async function run() {
  const api = new SomeProvider();

  const res = await api.validateConnection(
    {
      instanceId: "123",
    },
    ["123"]
  );

  if (res.status === 200) {
    const data = await res.json();
  }

  return res;
}
