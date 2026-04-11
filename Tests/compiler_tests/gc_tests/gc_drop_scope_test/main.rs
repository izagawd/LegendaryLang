fn main() -> i32 {
    let result: i32 = {
        let b: Gc(i32) = Gc.New(42);
        *b
    };
    result
}
