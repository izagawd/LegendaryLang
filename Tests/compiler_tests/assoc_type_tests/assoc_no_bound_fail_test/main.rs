use Std.Ops.Add;
fn dd[T:! Sized](one: T) -> (T as Add(T)).Output {
    one
}
fn main() -> i32 {
    dd(5)
}
