use Std.Ops.Add;
fn something[T:! Sized +Add(T)](one: T, two: T) -> (T as Add(T)).Output {
    one + two
}

fn another[T:! Sized +Add(T)](one: T, two: T) -> T.Output {
    one + two
}

fn main() -> i32 {
    another(4, 4) + something(4, 4)
}
