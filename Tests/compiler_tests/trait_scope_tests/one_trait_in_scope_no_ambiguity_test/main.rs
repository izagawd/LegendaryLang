use Std.Ops.TryInto;

fn main() -> i32 {
    let x: i32 = 42;
    let r: Option(u8) = x.TryInto();
    match r {
        Option.Some(val) => 1,
        Option.None => 0
    }
}
