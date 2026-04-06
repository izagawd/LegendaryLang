fn fib(n: i32) -> i32 {
    let a = 0;
    let b = 1;
    let i = 0;
    while i < n {
        let tmp = b;
        b = a + b;
        a = tmp;
        i = i + 1;
    };
    a
}

fn main() -> i32 {
    fib(10)
}
