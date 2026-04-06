fn pow(base: i32, exp: i32) -> i32 {
    let result = 1;
    let i = 0;
    while i < exp {
        result = result * base;
        i = i + 1;
    };
    result
}

fn main() -> i32 {
    pow(2, 10)
}
