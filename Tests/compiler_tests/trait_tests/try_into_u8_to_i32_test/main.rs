use Std.Ops.TryInto;
use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let x: u8 = 123;
    let result: Option(i32) = x.TryInto();
    match result {
        Option.Some(val) => val,
        Option.None => 0 - 1
    }
}
