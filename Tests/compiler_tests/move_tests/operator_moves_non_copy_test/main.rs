use Std.Ops.Add;
use Std.Ops.Sub;
fn use_twice[T:! Sized +Add(T) + Sub(T)](one: T, two: T) -> T {
    one + two - two
}

fn main() -> i32 {
    use_twice(2, 2)
}
