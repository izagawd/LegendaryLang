fn main() -> i32 {
    let result: i32 = {
        let b: Box(i32) = Box.New(42);
        *b
    };
    result
}
