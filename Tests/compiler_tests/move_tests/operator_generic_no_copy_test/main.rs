use Std.Core.Ops.Add;
fn use_twice(T:! Add(T), a: T, b: T) -> i32 {
    let x = a + b;
    let y = a + b;
    5
}

fn main() -> i32 {
    use_twice(i32, 1, 2)
}
