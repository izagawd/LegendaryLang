fn main() -> i32 {
    let x = 10;
    let steps = 0;
    while x > 0 {
        x = x - 1;
        steps = steps + 1;
    };
    steps
}
