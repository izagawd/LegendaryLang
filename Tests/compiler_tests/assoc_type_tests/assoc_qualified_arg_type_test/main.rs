fn take_output(x: <i32 as Add<i32>>::Output) -> i32 {
    x
}
fn main() -> i32 {
    take_output(11)
}
