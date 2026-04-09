fn main() -> i32 {
    let x = Option.Some(42);
    match x {
        Std.Core.Option.Some(v) => v,
        Std.Core.Option.None => 0
    }
}
