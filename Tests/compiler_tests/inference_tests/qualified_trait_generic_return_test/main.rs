use Std.Ops.TryInto;

fn main() -> i32 {
    let made: usize = 42;
    match (usize as TryInto(i32)).TryInto(made) {
        Option.Some(gotten) => gotten,
        _ => 0
    }
}
