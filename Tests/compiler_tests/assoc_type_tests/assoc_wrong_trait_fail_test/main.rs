fn dd<T: Copy>(one: T) -> <T as Add<T>>::Output {
    one
}
fn main() -> i32 {
    dd(5)
}
