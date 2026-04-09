fn classify(x: i32) -> i32 {
    if x > 100 {
        return 3;
    }
    if x > 10 {
        return 2;
    }
    if x > 0 {
        return 1;
    }
    0
}
fn main() -> i32 {
    classify(0) + classify(5) + classify(50) + classify(200)
}
