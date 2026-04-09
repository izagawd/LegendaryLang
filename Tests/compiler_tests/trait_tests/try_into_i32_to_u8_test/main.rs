use Std.Ops.TryInto;
use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let x: i32 = 42;
    let result: Option(u8) = x.TryInto();
    match result {
        Option.Some(val) => 1,
        Option.None => 0
    }
}
