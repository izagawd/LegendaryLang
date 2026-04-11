use Std.Ops.Add;
fn dd[T:! Sized +Add(T, Output = T) + Copy](one: T, two: T) -> (T as Add(T)).Output {
    one + two
}
fn main() -> i32 {
    dd(3, 4)
}
