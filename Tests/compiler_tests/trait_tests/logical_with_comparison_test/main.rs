fn in_range(x: i32, low: i32, high: i32) -> bool {
    x > low && x < high
}

fn main() -> i32 {
    let a = in_range(5, 0, 10);
    let b = in_range(15, 0, 10);
    let c = in_range(0, 0, 10);
    let result = 0;
    if a { result = result + 1; };
    if b { result = result + 10; };
    if c { result = result + 100; };
    result
}
