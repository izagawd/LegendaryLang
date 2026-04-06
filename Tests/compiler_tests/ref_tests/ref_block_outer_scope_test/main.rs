fn main() -> i32 {
    let a = 42;
    let gotten = {
        &a
    };
    *gotten
}
