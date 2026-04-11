fn main() -> i32 {
    let result: i32 = {
        let b: GcMut(i32) = GcMut.New(42);
        *b
    };
    result
}
