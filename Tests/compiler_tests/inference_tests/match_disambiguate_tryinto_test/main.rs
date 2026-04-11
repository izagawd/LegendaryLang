use Std.Ops.TryInto;

fn main() -> i32 {
    let made: u8 = 5;
    match made.TryInto() {
        Option.Some(gotten) => gotten,
        _ => 0
    }
}
